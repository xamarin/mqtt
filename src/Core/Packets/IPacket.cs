namespace System.Net.Mqtt.Packets
{
	public interface IPacket
    {
        PacketType Type { get; }
    }
}
