using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Storage
{
	internal class PendingAcknowledgement
	{
		public MqttPacketType Type { get; set; }

		public ushort PacketId { get; set; }
	}
}
