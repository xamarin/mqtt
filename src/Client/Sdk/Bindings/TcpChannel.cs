﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Sdk.Bindings
{
	internal class TcpChannel : IMqttChannel<byte[]>
	{
        static readonly ITracer tracer = Tracer.Get<TcpChannel> ();

        bool disposed;

		readonly TcpClient client;
		readonly IPacketBuffer buffer;
		readonly ReplaySubject<byte[]> receiver;
		readonly ReplaySubject<byte[]> sender;
		readonly IDisposable streamSubscription;

		public TcpChannel (TcpClient client, 
			IPacketBuffer buffer,
			MqttConfiguration configuration)
		{
			this.client = client;
			this.client.ReceiveBufferSize = configuration.BufferSize;
			this.client.SendBufferSize = configuration.BufferSize;
			this.buffer = buffer;
			receiver = new ReplaySubject<byte[]> (window: TimeSpan.FromSeconds (configuration.WaitTimeoutSecs));
			sender = new ReplaySubject<byte[]> (window: TimeSpan.FromSeconds (configuration.WaitTimeoutSecs));
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

		public IObservable<byte[]> ReceiverStream { get { return receiver; } }

		public IObservable<byte[]> SenderStream { get { return sender; } }

		public async Task SendAsync (byte[] message)
		{
			if (disposed) {
				throw new ObjectDisposedException (GetType ().FullName);
			}

			if (!IsConnected) {
				throw new MqttException (Properties.Resources.MqttChannel_ClientNotConnected);
			}

			sender.OnNext (message);

			try {
				tracer.Verbose (Properties.Resources.MqttChannel_SendingPacket, message.Length);

				await client.GetStream ()
					.WriteAsync (message, 0, message.Length)
					.ConfigureAwait (continueOnCapturedContext: false);
			} catch (ObjectDisposedException disposedEx) {
				throw new MqttException (Properties.Resources.MqttChannel_StreamDisconnected, disposedEx);
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
				tracer.Info (Properties.Resources.Mqtt_Disposing, GetType ().FullName);

				streamSubscription.Dispose ();
				receiver.OnCompleted ();

                try {
                    client?.Dispose ();
                } catch (SocketException socketEx) {
                    tracer.Error (socketEx, Properties.Resources.MqttChannel_DisposeError, socketEx.SocketErrorCode);
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
				.Select (x => buffer.Take (x));
			})
			.Repeat ()
			.TakeWhile (bytes => bytes.Any ())
			.ObserveOn (NewThreadScheduler.Default)
			.Subscribe (bytes => {
				var packets = default (IEnumerable<byte[]>);

				if (buffer.TryGetPackets (bytes, out packets)) {
					foreach (var packet in packets) {
						tracer.Verbose (Properties.Resources.MqttChannel_ReceivedPacket, packet.Length);

						receiver.OnNext (packet);
					}
				}
			}, ex => {
				if (ex is ObjectDisposedException) {
					receiver.OnError (new MqttException (Properties.Resources.MqttChannel_StreamDisconnected, ex));
				} else {
					receiver.OnError (ex);
				}
			}, () => {
				tracer.Warn (Properties.Resources.MqttChannel_NetworkStreamCompleted);
				receiver.OnCompleted ();
			});
		}
	}
}
