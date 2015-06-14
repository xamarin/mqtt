namespace System.Net.Mqtt.Packets
{
	public class Disconnect : IPacket
    {
		public PacketType Type { get { return PacketType.Disconnect; }}
    }
}
