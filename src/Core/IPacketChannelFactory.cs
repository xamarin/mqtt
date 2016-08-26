using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt
{
	internal interface IPacketChannelFactory
	{
		IChannel<IPacket> Create ();

		IChannel<IPacket> Create (IChannel<byte[]> binaryChannel);
	}
}
