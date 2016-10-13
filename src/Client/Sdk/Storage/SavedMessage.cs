using Hermes.Packets;

namespace Hermes.Storage
{
	public class SavedMessage
	{
		public QualityOfService QualityOfService { get; set; }

		public string Topic { get; set; }

		public ushort? PacketId { get; set; }

		public byte[] Payload { get; set; }
	}
}
