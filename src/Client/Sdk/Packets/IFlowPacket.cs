namespace System.Net.Mqtt.Sdk.Packets
{
	internal interface IFlowPacket : IPacket
    {
        ushort PacketId { get; }
    }
}
