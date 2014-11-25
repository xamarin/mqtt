using System;
using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes
{
	public interface ICommunicationContext : IDisposable
	{
		bool IsFaulted { get; }

		IObservable<IPacket> PendingDeliveries { get; }

		Task PushDeliveryAsync (IPacket packet);

		void PushError (ProtocolException exception);

		void PushError (string message);

		void PushError (string message, Exception exception);
	}
}
