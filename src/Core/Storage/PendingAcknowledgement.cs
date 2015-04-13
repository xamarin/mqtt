using Hermes.Packets;

namespace Hermes.Storage
{
	public class PendingAcknowledgement
	{
		public PacketType Type { get; set; }

		public ushort PacketId { get; set; }
	}
}
