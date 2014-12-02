using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes
{
	public class PacketChannel : IChannel<IPacket>
	{
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
			}, onError: ex => this.receiver.OnError(ex), onCompleted: () => this.receiver.OnCompleted());
		}

		public bool IsConnected { get { return innerChannel != null && innerChannel.IsConnected; } }

		public IObservable<IPacket> Receiver { get { return this.receiver; } }

		public IObservable<IPacket> Sender { get { return this.sender; } }

		public async Task SendAsync (IPacket packet)
		{
			try {
				var bytes = await this.manager.GetBytesAsync (packet);

				this.sender.OnNext (packet);

				await this.innerChannel.SendAsync (bytes);
			} catch (ProtocolException ex) {
				this.sender.OnError (ex);
			}
		}

		public void Close ()
		{
			this.subscription.Dispose ();
			this.innerChannel.Close ();
			this.receiver.OnCompleted ();
			this.sender.OnCompleted ();
		}
	}
}
