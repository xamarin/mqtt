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
				var packet = await this.manager.GetAsync(bytes);

				this.subject.OnNext (packet); 
			}, onError: ex => this.subject.OnError(ex), onCompleted: () => this.subject.OnCompleted());
		}

		public IObservable<IPacket> Receiver { get { return this.subject; } }

		public async Task SendAsync (IPacket packet)
		{
			var bytes = await this.manager.GetAsync (packet);

			await this.innerChannel.SendAsync (bytes);
		}

		public void Close ()
		{
			this.subscription.Dispose ();
			this.subject.Dispose ();
		}
	}
}
