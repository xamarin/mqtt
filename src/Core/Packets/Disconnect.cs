namespace System.Net.Mqtt.Packets
{
	internal class Disconnect : IPacket
	{
		public PacketType Type { get { return PacketType.Disconnect; } }
	}
}
