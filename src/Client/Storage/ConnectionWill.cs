using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Storage
{
	internal class ConnectionWill : StorageObject
	{
		public string ClientId { get; set; }

		public MqttLastWill Will { get; set; }
	}
}
