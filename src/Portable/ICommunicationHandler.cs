using Hermes.Packets;

namespace Hermes
{
	public interface ICommunicationHandler
	{
		ICommunicationContext Handle (IChannel<IPacket> channel);
	}
}
