using System.Collections.Generic;
using Hermes.Packets;

namespace Hermes
{
	public interface IConnectionProvider
    {
		int Connections { get; }

		IEnumerable<string> ActiveClients { get; }

		void AddConnection (string clientId, IChannel<IPacket> connection);

		IChannel<IPacket> GetConnection (string clientId);

		void RemoveConnection(string clientId);
    }
}
