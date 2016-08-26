namespace System.Net.Mqtt.Packets
{
	internal interface IPacket
    {
        MqttPacketType Type { get; }
    }
}
