using System.Net.Mqtt.Sdk.Packets;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Sdk
{
	internal interface IPacketChannelFactory
	{
		Task<IMqttChannel<IPacket>> CreateAsync ();

		IMqttChannel<IPacket> Create (IMqttChannel<byte[]> binaryChannel);
	}
}
