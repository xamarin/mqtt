namespace System.Net.Mqtt.Packets
{
	internal interface IFlowPacket : IPacket
    {
        ushort PacketId { get; }
    }
}
