namespace Hermes.Packets
{
	public class PingRequest : IPacket
    {
		public PacketType Type { get { return PacketType.PingRequest; }}
	}
}
