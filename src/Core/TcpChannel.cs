using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Exceptions;

namespace System.Net.Mqtt
{
	internal class TcpChannel : IChannel<byte[]>
	{
		static readonly ITracer tracer = Tracer.Get<TcpChannel> ();

		bool disposed;

		readonly TcpClient client;
		readonly IPacketBuffer buffer;
		readonly ReplaySubject<byte[]> receiver;
		readonly ReplaySubject<byte[]> sender;
		readonly IDisposable streamSubscription;

		public TcpChannel (TcpClient client, IPacketBuffer buffer, ProtocolConfiguration configuration)
		{
			this.client = client;
			this.client.ReceiveBufferSize = configuration.BufferSize;
			this.client.SendBufferSize = configuration.BufferSize;
			this.buffer = buffer;
			this.receiver = new ReplaySubject<byte[]> (window: TimeSpan.FromSeconds(configuration.WaitingTimeoutSecs));
			this.sender = new ReplaySubject<byte[]> (window: TimeSpan.FromSeconds(configuration.WaitingTimeoutSecs));
			this.streamSubscription = this.SubscribeStream ();
		}

		public bool IsConnected 
		{ 
			get 
			{
				var connected = !disposed;
				
				try {
					connected = connected && this.client.Connected;
				} catch (Exception) {
					connected = false;
				}

				return connected;
			} 
		}

		public IObservable<byte[]> Receiver { get { return this.receiver; } }

		public IObservable<byte[]> Sender { get { return this.sender; } }

		public async Task SendAsync (byte[] message)
		{
			if (this.disposed) {
				throw new ObjectDisposedException (this.GetType().FullName);
			}

			if (!this.IsConnected) {
				throw new MqttException (Properties.Resources.TcpChannel_ClientIsNotConnected);
			}

			this.sender.OnNext (message);

			try {
				tracer.Verbose (Properties.Resources.Tracer_TcpChannel_SendingPacket, message.Length);

				await this.client.GetStream()
					.WriteAsync(message, 0, message.Length)
					.ConfigureAwait(continueOnCapturedContext: false);
			} catch (ObjectDisposedException disposedEx) {
				throw new MqttException (Properties.Resources.TcpChannel_SocketDisconnected, disposedEx);
			}
		}

		public void Dispose ()
		{
			this.Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (this.disposed) return;

			if (disposing) {
				tracer.Info (Properties.Resources.Tracer_Disposing, this.GetType ().FullName);

				this.streamSubscription.Dispose ();
				this.receiver.OnCompleted ();

				if (this.IsConnected) {
					try {
						this.client.Client.Shutdown (SocketShutdown.Both);
						this.client.Close ();
					} catch (SocketException socketEx) {
						tracer.Error (socketEx, Properties.Resources.Tracer_TcpChannel_DisposeError, socketEx.ErrorCode);
					}
				}

				this.disposed = true;
			}
		}

		private IDisposable SubscribeStream()
		{
			return Observable.Defer(() => {
				var buffer = new byte[this.client.ReceiveBufferSize];

				return Observable.FromAsync<int>(() => {
					return this.client.GetStream().ReadAsync (buffer, 0, buffer.Length);
				})
				.Select(x => buffer.Take(x).ToArray());
			})
			.Repeat()
			.TakeWhile(bytes => bytes.Any())
			.ObserveOn(NewThreadScheduler.Default)
			.Subscribe(bytes => {
				var packets = default(IEnumerable<byte[]>);

				if (this.buffer.TryGetPackets (bytes, out packets)) {
					foreach (var packet in packets) {
						tracer.Verbose (Properties.Resources.Tracer_TcpChannel_ReceivedPacket, packet.Length);

						this.receiver.OnNext (packet);
					}
				}
			}, ex => {
				if (ex is ObjectDisposedException) {
					this.receiver.OnError (new MqttException (Properties.Resources.TcpChannel_SocketDisconnected, ex));
				} else {
					this.receiver.OnError (ex);
				}
			}, () => {
				tracer.Warn (Properties.Resources.Tracer_TcpChannel_NetworkStreamCompleted);
				this.receiver.OnCompleted ();
			});
		}
	}
}
