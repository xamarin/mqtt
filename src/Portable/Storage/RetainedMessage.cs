using Hermes.Packets;

namespace Hermes.Storage
{
	public class RetainedMessage
	{
		public QualityOfService QualityOfService { get; set; }

		public string Topic { get; set; }

		public string Payload { get; set; }
	}
}
