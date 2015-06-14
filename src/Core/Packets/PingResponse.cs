namespace System.Net.Mqtt.Packets
{
	public class PingResponse : IPacket
    {
		public PacketType Type { get { return PacketType.PingResponse; }}
    }
}
