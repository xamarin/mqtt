using Hermes.Packets;

namespace Hermes
{
	public interface IPacketChannelFactory
	{
		IChannel<IPacket> CreateChannel (IBufferedChannel<byte> socket);
	}
}
