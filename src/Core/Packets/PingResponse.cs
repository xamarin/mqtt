namespace Hermes.Packets
{
	public class PingResponse : IPacket
    {
		public PacketType Type { get { return PacketType.PingResponse; }}
    }
}
