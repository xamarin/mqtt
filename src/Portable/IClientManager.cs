using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes
{
	public interface IClientManager
    {
		void Add (string clientId, IChannel<IPacket> connection);

		Task SendMessageAsync (string clientId, IPacket packet);

        void Remove(string clientId);
    }
}
