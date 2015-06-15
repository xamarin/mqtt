namespace System.Net.Mqtt.Packets
{
	internal interface IPacket
    {
        PacketType Type { get; }
    }
}
