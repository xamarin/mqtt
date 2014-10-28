using System;
using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes
{
	public interface IProtocolConnection
	{
	    Guid Id { get; }

		string ClientId { get; }

		bool IsPending { get; }

		void Confirm (string clientId);

		Task SendAsync (IPacket packet);

		void Disconnect ();
	}
}
