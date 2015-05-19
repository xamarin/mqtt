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
			var topicFilter = "test/topic1/#";

			await client.SubscribeAsync (topicFilter, QualityOfService.AtMostOnce)
				.ConfigureAwait(continueOnCapturedContext: false);

			Assert.True (client.IsConnected);

			await client.UnsubscribeAsync (topicFilter);
		}

		[Fact]
		public async Task when_subscribe_multiple_topics_then_succeeds()
		{
			var client = this.GetClient ();
			var topicsToSubscribe = this.GetTestLoad();
			var topics = new List<string> ();

			for (var i = 1; i <= topicsToSubscribe; i++) {
				var topicFilter = string.Format ("test/topic{0}", i);

				await client.SubscribeAsync (topicFilter, QualityOfService.AtMostOnce)
					.ConfigureAwait(continueOnCapturedContext: false);

				topics.Add (topicFilter);
			}

			Assert.True (client.IsConnected);

			await client.UnsubscribeAsync (topics.ToArray ());
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
				await client.SubscribeAsync (topicFilter, QualityOfService.AtMostOnce)
					.ConfigureAwait(continueOnCapturedContext: false);
			}

			await client.UnsubscribeAsync (topics.ToArray())
				.ConfigureAwait(continueOnCapturedContext: false);

			Assert.True (client.IsConnected);

			client.Close ();
		}

		public void Dispose ()
		{
			this.server.Stop ();
		}
	}
}
