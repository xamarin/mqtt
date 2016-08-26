using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Storage
{
	internal class ClientSubscription
	{
		public string ClientId { get; set; }

		public string TopicFilter { get; set; }

		public MqttQualityOfService MaximumQualityOfService { get; set; }
	}
}
