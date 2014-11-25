using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes
{
	public class ProtocolChannel : IChannel<IPacket>
	{
		readonly Subject<IPacket> sender;
		readonly IChannel<IPacket> innerChannel;

		public ProtocolChannel (IChannel<IPacket> innerChannel)
		{
			this.sender = new Subject<IPacket> ();
			this.innerChannel = innerChannel;
		}

		public IObservable<IPacket> Receiver { get { return this.innerChannel.Receiver; } }

		public IObservable<IPacket> Sender { get { return this.sender; } }

		public async Task SendAsync (IPacket message)
		{
			await this.innerChannel.SendAsync (message);

			this.sender.OnNext (message);
		}

		public void NotifyError(Exception exception)
		{
			this.sender.OnError (exception);
		}

		public void NotifyError(string message)
		{
			this.NotifyError (new ProtocolException (message));
		}

		public void NotifyError(string message, Exception exception)
		{
			this.NotifyError (new ProtocolException (message, exception));
		}

		public void Close ()
		{
			this.innerChannel.Close ();
			this.sender.OnCompleted ();
		}
	}
}
