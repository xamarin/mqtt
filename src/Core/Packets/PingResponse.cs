namespace System.Net.Mqtt.Packets
{
	internal class PingResponse : IPacket
	{
		public MqttPacketType Type { get { return MqttPacketType.PingResponse; } }
	}
}
