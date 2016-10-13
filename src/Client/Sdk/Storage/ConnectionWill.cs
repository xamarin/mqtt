namespace System.Net.Mqtt.Sdk.Storage
{
	internal class ConnectionWill : StorageObject
	{
		public string ClientId { get; set; }

		public MqttLastWill Will { get; set; }
	}
}
