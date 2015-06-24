namespace System.Net.Mqtt.Packets
{
	internal class PingResponse : IPacket
    {
		public PacketType Type { get { return PacketType.PingResponse; }}
    }
}
