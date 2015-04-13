using System.Collections.Generic;
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
		public void when_subscribe_multiple_topics_then_succeeds()
		{
			var client = this.GetClient ();
			var topicsToSubscribe = 100;
			var subscribeTasks = new List<Task> ();

			for (var i = 1; i < topicsToSubscribe; i++) {
				var topicFilter = string.Format ("test/topic{0}", i);

				subscribeTasks.Add(client.SubscribeAsync (topicFilter, QualityOfService.AtMostOnce));
			}

			Task.WaitAll (subscribeTasks.ToArray());

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
