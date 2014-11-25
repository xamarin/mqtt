using System.Collections.Generic;
using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes
{
	public interface IClientManager
    {
		IEnumerable<string> Clients { get; }

		void AddClient (string clientId, IChannel<IPacket> connection);

		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task SendMessageAsync (string clientId, IPacket packet);

        void RemoveClient(string clientId);
    }
}
