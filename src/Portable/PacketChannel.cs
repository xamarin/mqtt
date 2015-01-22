using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes
{
	public class PacketChannel : IChannel<IPacket>
	{
		bool disposed;

		readonly IChannel<byte[]> innerChannel;
		readonly IPacketManager manager;
        readonly Subject<IPacket> receiver;
		readonly Subject<IPacket> sender;
		readonly IDisposable subscription;

		public PacketChannel (IChannel<byte[]> innerChannel, IPacketManager manager)
		{
			this.innerChannel = innerChannel;
			this.manager = manager;

			this.receiver = new Subject<IPacket> ();
			this.sender = new Subject<IPacket> ();
			this.subscription = innerChannel.Receiver.Subscribe (async bytes => {
				try {
					var packet = await this.manager.GetPacketAsync(bytes);

					this.receiver.OnNext (packet); 
				} catch (ProtocolException ex) {
					this.receiver.OnError (ex);
				}
			}, onError: ex => this.receiver.OnError(ex));
		}

		public bool IsConnected { get { return innerChannel != null && innerChannel.IsConnected; } }

		public IObservable<IPacket> Receiver { get { return this.receiver; } }

		public IObservable<IPacket> Sender { get { return this.sender; } }

		public async Task SendAsync (IPacket packet)
		{
			if (this.disposed)
				throw new ObjectDisposedException (this.GetType ().FullName);

			var bytes = await this.manager.GetBytesAsync (packet);

			this.sender.OnNext (packet);

			await this.innerChannel.SendAsync (bytes);
		}

		public void Dispose ()
		{
			this.Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (this.disposed) return;

			if (disposing) {
				this.subscription.Dispose ();
				this.innerChannel.Dispose ();
				this.receiver.OnCompleted ();
				this.sender.OnCompleted ();
				this.disposed = true;
			}
		}
	}
}
