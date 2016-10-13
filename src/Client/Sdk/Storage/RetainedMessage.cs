namespace System.Net.Mqtt.Sdk.Storage
{
	internal class RetainedMessage : StorageObject
	{
		public MqttQualityOfService QualityOfService { get; set; }

		public string Topic { get; set; }

		public byte[] Payload { get; set; }
	}
}
