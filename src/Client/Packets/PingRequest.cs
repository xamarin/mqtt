namespace System.Net.Mqtt.Packets
{
	internal class PingRequest : IPacket
	{
		public MqttPacketType Type { get { return MqttPacketType.PingRequest; } }
	}
}
