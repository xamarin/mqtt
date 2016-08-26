using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Storage
{
	internal class RetainedMessage : StorageObject
	{
		public MqttQualityOfService QualityOfService { get; set; }

		public string Topic { get; set; }

		public byte[] Payload { get; set; }
	}
}
