namespace System.Net.Mqtt.Sdk.Packets
{
	internal class PingRequest : IPacket
	{
		public MqttPacketType Type { get { return MqttPacketType.PingRequest; } }
	}
}
