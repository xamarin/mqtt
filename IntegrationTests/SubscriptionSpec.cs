using System.Threading.Tasks;
using Hermes.Packets;
using IntegrationTests.Context;
using Xunit;

namespace IntegrationTests
{
	public class SubscriptionSpec : ConnectedContext
	{
		[Fact]
		public async Task when_subscribe_topic_then_succeeds()
		{
			var client = this.GetClient ();
			var topicFilter = "test/topic1/*";

			await client.SubscribeAsync (topicFilter, QualityOfService.AtMostOnce);

			Assert.True (client.IsConnected);
		}

		[Fact]
		public async Task when_unsubscribe_topic_then_succeeds()
		{
			var client = this.GetClient ();
			var topicFilter1 = "test/topic1/*";
			var topicFilter2 = "test/topic2";

			await client.SubscribeAsync (topicFilter1, QualityOfService.AtMostOnce);
			await client.SubscribeAsync (topicFilter2, QualityOfService.ExactlyOnce);

			await client.UnsubscribeAsync (topicFilter1, topicFilter2);

			Assert.True (client.IsConnected);
		}
	}
}
