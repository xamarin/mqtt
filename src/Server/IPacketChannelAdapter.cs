using Hermes.Packets;

namespace Hermes
{
	public interface IPacketChannelAdapter
	{
		IChannel<IPacket> Adapt (IChannel<IPacket> channel);
	}
}
