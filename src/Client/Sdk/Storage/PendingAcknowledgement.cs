using System.Net.Mqtt.Sdk.Packets;

namespace System.Net.Mqtt.Sdk.Storage
{
	internal class PendingAcknowledgement
	{
		public MqttPacketType Type { get; set; }

		public ushort PacketId { get; set; }
	}
}
