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
        readonly IDisposable subscription;
        readonly Subject<IPacket> subject = new Subject<IPacket>();

		public PacketChannel (IChannel<byte[]> innerChannel, IPacketManager manager)
		{
			this.innerChannel = innerChannel;
			this.manager = manager;
			this.subscription = innerChannel.Receiver.Subscribe (async bytes => {
				//TODO: Proposal
				//var packet = await this.converter.ConvertAsync(bytes);

				//this.subject.OnNext (packet); 
				await this.manager.ManageAsync (bytes);
			}, onError: e => this.subject.OnError(e), onCompleted: () => this.subject.OnCompleted());
		}

		public IObservable<IPacket> Receiver { get { return this.subject; } }

		public async Task SendAsync (IPacket packet)
		{
			//TODO: 
			//This method should be used only to send packets to the client through the network
			//This method will be called by the PacketManager, and it's WRONG
			//We need some place to receive the packets sent by the PacketManager doing: this.subject.OnNext (packet); 
			//This will allow the packets to be collected by the observers and then passed to the flow manager

			//TODO: Proposal
			//var bytes = await this.converter.ConvertAsync(packet);

			//this.innerChannel.SendAsync (bytes);

			await this.manager.ManageAsync (packet);
		}

		public void Close ()
		{
			this.subscription.Dispose ();
			this.subject.Dispose ();
		}
	}
}
