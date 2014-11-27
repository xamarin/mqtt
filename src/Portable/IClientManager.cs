using System.Collections.Generic;
using Hermes.Packets;

namespace Hermes
{
	public interface IClientManager
    {
		IEnumerable<string> Clients { get; }

		void AddClient (string clientId, IChannel<IPacket> connection);

		/// <exception cref="ProtocolException">ProtocolException</exception>
		IChannel<IPacket> GetConnection (string clientId);

		/// <exception cref="ProtocolException">ProtocolException</exception>
		void RemoveClient(string clientId);
    }
}
