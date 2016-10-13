namespace System.Net.Mqtt.Sdk.Packets
{
	internal interface IPacket
    {
        MqttPacketType Type { get; }
    }
}
