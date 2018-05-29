namespace System.Net.Mqtt.Sdk.Storage
{
	internal class ConnectionWill : IStorageObject
	{
		public ConnectionWill (string clientId, MqttLastWill will)
		{
			Id = clientId;
			Will = will;
		}

		public string Id { get; set; }

		public MqttLastWill Will { get; set; }
	}
}
