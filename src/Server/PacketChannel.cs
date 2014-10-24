using System;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes
{
	public class PacketChannel : IChannel<IPacket>
	{
		readonly IEventStream events;
        readonly IDisposable subscription;
        readonly Subject<IPacket> subject = new Subject<IPacket>();

		public PacketChannel (IEventStream events)
		{
			this.events = events;
			this.subscription = events.Of<IPacket>()
                .Subscribe(message => subject.OnNext(message));

            this.Received = subject;
		}

		public IObservable<IPacket> Received { get; private set; }

		public async Task SendAsync (IPacket packet)
		{
			await Task.Run (() => this.events.Push (packet));
		}

		public void Close ()
		{
			this.subscription.Dispose ();
			this.subject.Dispose ();
		}
	}
}
