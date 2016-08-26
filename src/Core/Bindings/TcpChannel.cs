﻿using System.Collections.Generic;
using System.Linq;
using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Exceptions;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Bindings
{
	internal class TcpChannel : IMqttChannel<byte[]>
	{
		bool disposed;

		readonly ITracer tracer;
		readonly TcpClient client;
		readonly IPacketBuffer buffer;
		readonly ReplaySubject<byte[]> receiver;
		readonly ReplaySubject<byte[]> sender;
		readonly IDisposable streamSubscription;

		public TcpChannel (TcpClient client, 
			IPacketBuffer buffer,
			ITracerManager tracerManager, 
			MqttConfiguration configuration)
		{
			tracer = tracerManager.Get<TcpChannel> ();
			this.client = client;
			this.client.ReceiveBufferSize = configuration.BufferSize;
			this.client.SendBufferSize = configuration.BufferSize;
			this.buffer = buffer;
			receiver = new ReplaySubject<byte[]> (window: TimeSpan.FromSeconds (configuration.WaitingTimeoutSecs));
			sender = new ReplaySubject<byte[]> (window: TimeSpan.FromSeconds (configuration.WaitingTimeoutSecs));
			streamSubscription = SubscribeStream ();
		}

		public bool IsConnected
		{
			get
			{
				var connected = !disposed;

				try {
					connected = connected && client.Connected;
				} catch (Exception) {
					connected = false;
				}

				return connected;
			}
		}

		public IObservable<byte[]> Receiver { get { return receiver; } }

		public IObservable<byte[]> Sender { get { return sender; } }

		public async Task SendAsync (byte[] message)
		{
			if (disposed) {
				throw new ObjectDisposedException (GetType ().FullName);
			}

			if (!IsConnected) {
				throw new MqttException (Resources.TcpChannel_ClientIsNotConnected);
			}

			sender.OnNext (message);

			try {
				tracer.Verbose (Resources.TcpChannel_SendingPacket, message.Length);

				await client.GetStream ()
					.WriteAsync (message, 0, message.Length)
					.ConfigureAwait (continueOnCapturedContext: false);
			} catch (ObjectDisposedException disposedEx) {
				throw new MqttException (Resources.TcpChannel_SocketDisconnected, disposedEx);
			}
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposed) return;

            if (disposing) {
				tracer.Info (Resources.Tracer_Disposing, GetType ().FullName);

				streamSubscription.Dispose ();
				receiver.OnCompleted ();

                try {
                    client?.Dispose ();
                } catch (SocketException socketEx) {
                    tracer.Error (socketEx, Resources.Tracer_TcpChannel_DisposeError, socketEx.SocketErrorCode);
                }

                disposed = true;
			}
		}

		IDisposable SubscribeStream ()
		{
			return Observable.Defer (() => {
				var buffer = new byte[client.ReceiveBufferSize];

				return Observable.FromAsync<int> (() => {
					return client.GetStream ().ReadAsync (buffer, 0, buffer.Length);
				})
				.Select (x => buffer.Take (x).ToArray ());
			})
			.Repeat ()
			.TakeWhile (bytes => bytes.Any ())
			.ObserveOn (NewThreadScheduler.Default)
			.Subscribe (bytes => {
				var packets = default(IEnumerable<byte[]>);

				if (buffer.TryGetPackets (bytes, out packets)) {
					foreach (var packet in packets) {
						tracer.Verbose (Resources.TcpChannel_ReceivedPacket, packet.Length);

						receiver.OnNext (packet);
					}
				}
			}, ex => {
				if (ex is ObjectDisposedException) {
					receiver.OnError (new MqttException (Resources.TcpChannel_SocketDisconnected, ex));
				} else {
					receiver.OnError (ex);
				}
			}, () => {
				tracer.Warn (Resources.TcpChannel_NetworkStreamCompleted);
				receiver.OnCompleted ();
			});
		}
	}
}
