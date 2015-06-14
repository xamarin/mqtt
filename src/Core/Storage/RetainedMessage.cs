using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Storage
{
	public class RetainedMessage : StorageObject
	{
		public QualityOfService QualityOfService { get; set; }

		public string Topic { get; set; }

		public byte[] Payload { get; set; }
	}
}
