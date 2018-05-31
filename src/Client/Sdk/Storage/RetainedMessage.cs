namespace System.Net.Mqtt.Sdk.Storage
{
	internal class RetainedMessage : IStorageObject
	{
		public RetainedMessage (string topic, MqttQualityOfService qos, byte[] payload)
		{
			Id = topic;
			QualityOfService = qos;
			Payload = payload;
		}

		public string Id { get; }

		public MqttQualityOfService QualityOfService { get; }

		public byte[] Payload { get; }
	}
}
