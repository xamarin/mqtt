using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hermes;
using Hermes.Packets;
using IntegrationTests.Context;
using Xunit;

namespace IntegrationTests
{
	public class SubscriptionSpec : ConnectedContext, IDisposable
	{
		private readonly Server server;

		public SubscriptionSpec () : base()
		{
			this.server = this.GetServer ();
		}

		[Fact]
		public async Task when_subscribe_topic_then_succeeds()
		{
			var client = this.GetClient ();
			var topicFilter = "test/topic1/*";

			await client.SubscribeAsync (topicFilter, QualityOfService.AtMostOnce);

			Assert.True (client.IsConnected);
		}

		[Fact]
		public async Task when_subscribe_multiple_topics_then_succeeds()
		{
			var client = this.GetClient ();
			var topicsToSubscribe = this.GetTestLoad();

			for (var i = 1; i <= topicsToSubscribe; i++) {
				var topicFilter = string.Format ("test/topic{0}", i);

				await client.SubscribeAsync (topicFilter, QualityOfService.AtMostOnce);
			}

			Assert.True (client.IsConnected);

			client.Close ();
		}

		[Fact]
		public async Task when_unsubscribe_topic_then_succeeds()
		{
			var client = this.GetClient ();
			var topicsToSubscribe = this.GetTestLoad();
			var topics = new List<string> ();

			for (var i = 1; i <= topicsToSubscribe; i++) {
				var topicFilter = string.Format ("test/topic{0}", i);

				topics.Add (topicFilter);
				await client.SubscribeAsync (topicFilter, QualityOfService.AtMostOnce);
			}

			await client.UnsubscribeAsync (topics.ToArray());

			Assert.True (client.IsConnected);

			client.Close ();
		}

		public void Dispose ()
		{
			this.server.Stop ();
		}
	}
}
