using Hermes.Packets;

namespace Hermes
{
	public interface IMessagingHandler
	{
		void Handle (IChannel<IPacket> channel);
	}
}
