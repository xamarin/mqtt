using Hermes.Packets;

namespace Hermes
{
	public interface IPacketChannelFactory
	{
		IChannel<IPacket> Create (IChannel<byte[]> binaryChannel);
	}
}
