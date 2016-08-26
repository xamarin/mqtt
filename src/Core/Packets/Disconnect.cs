namespace System.Net.Mqtt.Packets
{
	internal class Disconnect : IPacket
	{
		public MqttPacketType Type { get { return MqttPacketType.Disconnect; } }
	}
}
