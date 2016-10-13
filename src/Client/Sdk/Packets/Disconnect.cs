namespace System.Net.Mqtt.Sdk.Packets
{
	internal class Disconnect : IPacket
	{
		public MqttPacketType Type { get { return MqttPacketType.Disconnect; } }
	}
}
