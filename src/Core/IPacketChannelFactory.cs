using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt
{
	public interface IPacketChannelFactory
	{
		IChannel<IPacket> Create ();

		IChannel<IPacket> Create (IChannel<byte[]> binaryChannel);
	}
}
