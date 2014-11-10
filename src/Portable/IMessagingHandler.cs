using Hermes.Packets;

namespace Hermes
{
	public interface IMessagingHandler
	{
		void Handle (string clientId, IChannel<IPacket> channel);
	}
}
