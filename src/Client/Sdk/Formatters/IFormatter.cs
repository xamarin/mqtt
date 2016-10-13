using System.Threading.Tasks;
using System.Net.Mqtt.Sdk.Packets;

namespace System.Net.Mqtt.Sdk.Formatters
{
	internal interface IFormatter
	{
		MqttPacketType PacketType { get; }

		Task<IPacket> FormatAsync (byte[] bytes);

		Task<byte[]> FormatAsync (IPacket packet);
	}
}
