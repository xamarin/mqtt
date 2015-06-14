using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Storage
{
	public class ClientSubscription
	{
		public string ClientId { get; set; }

		public string TopicFilter { get; set; }

		public QualityOfService MaximumQualityOfService { get; set; }
	}
}
