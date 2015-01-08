using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes
{
	public class ProtocolChannel : IChannel<IPacket>
	{
		bool disposed;

		readonly Subject<IPacket> sender;
		readonly IChannel<IPacket> innerChannel;

		public ProtocolChannel (IChannel<IPacket> innerChannel)
		{
			this.sender = new Subject<IPacket> ();
			this.innerChannel = innerChannel;
		}

		public bool IsConnected { get { return innerChannel != null && innerChannel.IsConnected; } }

		public IObservable<IPacket> Receiver { get { return this.innerChannel.Receiver; } }

		public IObservable<IPacket> Sender { get { return this.sender; } }

		public async Task SendAsync (IPacket message)
		{
			if (this.disposed)
				throw new ObjectDisposedException (this.GetType ().FullName);

			this.sender.OnNext (message);

			await this.innerChannel.SendAsync (message);
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

		public void Dispose ()
		{
			this.Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (this.disposed) return;

			if (disposing) {
				this.innerChannel.Dispose ();
				this.sender.OnCompleted ();
				this.disposed = true;
			}
		}
	}
}
