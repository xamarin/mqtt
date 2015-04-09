namespace Hermes.Packets
{
	public class Disconnect : IPacket
    {
		public PacketType Type { get { return PacketType.Disconnect; }}
    }
}
