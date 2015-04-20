using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Hermes.Diagnostics;
using Hermes.Properties;

namespace Hermes
{
	public class TcpChannel : IChannel<byte[]>
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
			this.streamSubscription = this.GetStreamSubscription (this.client);
		}

		public bool IsConnected 
		{ 
			get 
			{
				var connected = this.client != null;
				
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
				throw new ProtocolException (Resources.TcpChannel_ClientIsNotConnected);
			}

			this.sender.OnNext (message);

			try {
				tracer.Info (Resources.Tracer_TcpChannel_SendingPacket, DateTime.Now.ToString ("MM/dd/yyyy hh:mm:ss.fff"), message.Length);

				await this.client.GetStream ().WriteAsync(message, 0, message.Length);
			} catch (ObjectDisposedException disposedEx) {
				throw new ProtocolException (Resources.TcpChannel_SocketDisconnected, disposedEx);
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
				this.streamSubscription.Dispose ();
				this.receiver.OnCompleted ();

				if (this.IsConnected) {
					this.client.Close ();
				}

				this.disposed = true;
			}
		}

		private IDisposable GetStreamSubscription(TcpClient client)
		{
			return Observable.Defer(() => {
				var buffer = new byte[client.ReceiveBufferSize];

				return Observable.FromAsync<int>(() => {
					return this.client.GetStream ().ReadAsync (buffer, 0, buffer.Length);
				})
				.Select(x => buffer.Take(x).ToArray());
			})
			.Repeat()
			.TakeWhile(bytes => bytes.Any())
			.Subscribe(bytes => {
				var packets = default(IEnumerable<byte[]>);

				if (this.buffer.TryGetPackets (bytes, out packets)) {
					foreach (var packet in packets) {
						tracer.Info (Resources.Tracer_TcpChannel_ReceivedPacket, DateTime.Now.ToString ("MM/dd/yyyy hh:mm:ss.fff"), packet.Length);

						this.receiver.OnNext (packet);
					}
				}
			}, ex => {
				if (ex is ObjectDisposedException) {
					this.receiver.OnError (new ProtocolException (Resources.TcpChannel_SocketDisconnected, ex));
				} else {
					this.receiver.OnError (ex);
				}
			}, () => {
				tracer.Warn (Resources.Tracer_TcpChannel_NetworkStreamCompleted, DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff"));

				this.Dispose ();
			});
		}
	}
}
