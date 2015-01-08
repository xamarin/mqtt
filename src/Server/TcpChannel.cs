using System;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Hermes.Properties;

namespace Hermes
{
	public class TcpChannel : IChannel<byte[]>
	{
		bool disposed;

		readonly object lockObject = new object ();
		readonly TcpClient client;
		readonly IPacketBuffer buffer;
		readonly Subject<byte[]> receiver;
		readonly Subject<byte[]> sender;
		readonly IDisposable subscription;

		public TcpChannel (TcpClient client, IPacketBuffer buffer)
			: this(client, buffer, receiveBufferSize: 8192)
		{
			// The default receive buffer size of TcpClient according to
			// http://msdn.microsoft.com/en-us/library/system.net.sockets.tcpclient.receivebuffersize.aspx
			// is 8192 bytes
		}

		public TcpChannel (TcpClient client, IPacketBuffer buffer, int receiveBufferSize)
		{
			if (!client.Connected)
            {
                throw new InvalidOperationException(Resources.TcpChannel_ClientMustBeConnected);
            }

			this.client = client;
			this.client.ReceiveBufferSize = receiveBufferSize;
			this.buffer = buffer;
			this.receiver = new Subject<byte[]> ();
			this.sender = new Subject<byte[]> ();
			this.subscription = this.GetStreamSubscription (this.client);
		}

		public bool IsConnected { get { return this.client != null && this.client.Connected; } }

		public IObservable<byte[]> Receiver { get { return this.receiver; } }

		public IObservable<byte[]> Sender { get { return this.sender; } }

		public async Task SendAsync (byte[] message)
		{
			if (this.disposed)
				throw new ObjectDisposedException (this.GetType().FullName);

			try {
				Monitor.Enter(lockObject);

				await this.client
					.GetStream ()
					.WriteAsync (message, 0, message.Length);

				this.sender.OnNext (message);
			} catch (Exception ex) {
				this.sender.OnError (ex);
			} finally { Monitor.Exit(lockObject); }
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
				this.subscription.Dispose ();
				this.client.Close ();
				this.receiver.OnCompleted ();
				this.sender.OnCompleted ();
				this.disposed = true;
			}
		}

		private IDisposable GetStreamSubscription(TcpClient client)
		{
			return Observable.Defer(() => {
				var buffer = new byte[client.ReceiveBufferSize];

				return Observable
					.FromAsync<int>(() => this.client.GetStream().ReadAsync(buffer, 0, buffer.Length))
					.Select(x => buffer.Take(x).ToArray());
			})
			.Repeat()
			.TakeWhile(bytes => bytes.Any())
			.Subscribe(bytes => {
				var packet = default (byte[]);

				if (this.buffer.TryGetPacket (bytes, out packet)) {
					this.receiver.OnNext (packet);
				}
			}, ex => this.receiver.OnError(ex), () => this.receiver.OnCompleted());
		}
	}
}
