using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes
{
	public class CommunicationContext : ICommunicationContext
	{
		readonly Subject<IPacket> pendingDeliveries;

		public CommunicationContext ()
		{
			this.pendingDeliveries = new Subject<IPacket> ();
		}

		public bool IsFaulted { get; private set; }

		public IObservable<IPacket> PendingDeliveries {  get { return this.pendingDeliveries; } }

		public Task PushDeliveryAsync(IPacket packet)
		{
			return Task.Run(() => this.pendingDeliveries.OnNext (packet));
		}

		public void PushError (ProtocolException exception)
		{
			this.IsFaulted = true;

			this.pendingDeliveries.OnError (exception);
		}

		public void PushError (string message)
		{
			this.PushError (new ProtocolException (message));
		}

		public void PushError (string message, Exception exception)
		{
			this.PushError (new ProtocolException (message, exception));
		}

		public void Dispose ()
		{
			this.Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposing) {
				this.pendingDeliveries.OnCompleted ();
			}
		}
	}
}
