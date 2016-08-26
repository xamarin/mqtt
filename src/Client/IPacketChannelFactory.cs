using System.Net.Mqtt.Packets;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
	internal interface IPacketChannelFactory
	{
		Task<IMqttChannel<IPacket>> CreateAsync ();

		IMqttChannel<IPacket> Create (IMqttChannel<byte[]> binaryChannel);
	}
}
