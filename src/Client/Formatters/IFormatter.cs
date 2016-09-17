using System.Threading.Tasks;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Formatters
{
	internal interface IFormatter
	{
		MqttPacketType PacketType { get; }

		Task<IPacket> FormatAsync (byte[] bytes);

		Task<byte[]> FormatAsync (IPacket packet);
	}
}
