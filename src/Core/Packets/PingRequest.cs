namespace System.Net.Mqtt.Packets
{
	internal class PingRequest : IPacket
	{
		public PacketType Type { get { return PacketType.PingRequest; } }
	}
}
