﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Mqtt.Packets;
using IntegrationTests.Context;
using Xunit;
using System.Net.Mqtt.Server;

namespace IntegrationTests
{
	public class SubscriptionSpec : ConnectedContext
	{
		readonly MqttServer server;

		public SubscriptionSpec ()
		{
			server = GetServer ();
		}

		[Fact]
		public async Task when_subscribe_topic_then_succeeds()
		{
			var client = GetClient ();
			var topicFilter = Guid.NewGuid ().ToString () + "/#";

			await client.SubscribeAsync (topicFilter, MqttQualityOfService.AtMostOnce)
				.ConfigureAwait(continueOnCapturedContext: false);

			Assert.True (client.IsConnected);

			await client.UnsubscribeAsync (topicFilter);

			client.Close ();
		}

		[Fact]
		public async Task when_subscribe_multiple_topics_then_succeeds()
		{
			var client = GetClient ();
			var topicsToSubscribe = GetTestLoad();
			var topics = new List<string> ();
			var tasks = new List<Task> ();

			for (var i = 1; i <= topicsToSubscribe; i++) {
				var topicFilter = Guid.NewGuid ().ToString ();

				tasks.Add (client.SubscribeAsync (topicFilter, MqttQualityOfService.AtMostOnce));
				topics.Add (topicFilter);
			}

			await Task.WhenAll (tasks);

			Assert.True (client.IsConnected);

			await client.UnsubscribeAsync (topics.ToArray ());

			client.Close ();
		}

		[Fact]
		public async Task when_unsubscribe_topic_then_succeeds()
		{
			var client = GetClient ();
			var topicsToSubscribe = GetTestLoad();
			var topics = new List<string> ();
			var tasks = new List<Task> ();

			for (var i = 1; i <= topicsToSubscribe; i++) {
				var topicFilter = Guid.NewGuid ().ToString ();

				tasks.Add (client.SubscribeAsync (topicFilter, MqttQualityOfService.AtMostOnce));
				topics.Add (topicFilter);
			}

			await Task.WhenAll (tasks);
			await client.UnsubscribeAsync (topics.ToArray())
				.ConfigureAwait(continueOnCapturedContext: false);

			Assert.True (client.IsConnected);

			client.Close ();
		}

		public void Dispose ()
		{
			if (server != null) {
				server.Stop ();
			}
		}
	}
}
