using System;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Hermes.Messages;

namespace Hermes
{
	public class MessageChannel : IChannel<IMessage>
	{
		readonly IEventStream events;
        readonly IDisposable subscription;
        readonly Subject<IMessage> subject = new Subject<IMessage>();

		public MessageChannel (IEventStream events)
		{
			this.events = events;
			this.subscription = events.Of<IMessage>()
                .Subscribe(message => subject.OnNext(message));

            this.Received = subject;
		}

		public IObservable<IMessage> Received { get; private set; }

		public async Task SendAsync (IMessage message)
		{
			await Task.Run (() => this.events.Push (message));
		}

		public void Close ()
		{
			this.subscription.Dispose ();
			this.subject.Dispose ();
		}
	}
}
