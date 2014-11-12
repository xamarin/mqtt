using Hermes.Packets;

namespace Hermes.Storage
{
	public class PendingMessage
	{
		public QualityOfService QualityOfService { get; set; }

		public string Topic { get; set; }

		public byte[] Payload { get; set; }
	}
}
