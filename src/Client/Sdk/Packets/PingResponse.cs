namespace System.Net.Mqtt.Sdk.Packets
{
	internal class PingResponse : IPacket
	{
		public MqttPacketType Type { get { return MqttPacketType.PingResponse; } }
	}
}
