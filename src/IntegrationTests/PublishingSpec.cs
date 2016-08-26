﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Mqtt.Packets;
using IntegrationTests.Context;
using IntegrationTests.Messages;
using Xunit;
using System.Text;
using System.Collections.Generic;
using System.Net.Mqtt.Server;
using System.Net.Mqtt.Client;

namespace IntegrationTests
{
	public class PublishingSpec : ConnectedContext, IDisposable
	{
		readonly Server server;

		public PublishingSpec () 
			: base(keepAliveSecs: 1)
		{
			server = GetServer ();
		}

		[Fact]
		public async Task when_publish_messages_with_qos0_then_succeeds()
		{
			var client = GetClient ();
			var topic = Guid.NewGuid ().ToString ();
			var count = GetTestLoad();
			var tasks = new List<Task> ();

			for (var i = 1; i <= count; i++) {
				var testMessage = GetTestMessage();
				var message = new ApplicationMessage
				{
					Topic = topic,
					Payload = Serializer.Serialize(testMessage)
				};

				tasks.Add (client.PublishAsync (message, QualityOfService.AtMostOnce));
			}

			await Task.WhenAll (tasks);

			Assert.True (client.IsConnected);

			client.Close ();
		}

		[Fact]
		public async Task when_publish_messages_with_qos1_then_succeeds()
		{
			var client = GetClient ();
			var topic = Guid.NewGuid ().ToString ();
			var count = GetTestLoad();
			var tasks = new List<Task> ();

			for (var i = 1; i <= count; i++) {
				var testMessage = GetTestMessage();
				var message = new ApplicationMessage
				{
					Topic = topic,
					Payload = Serializer.Serialize(testMessage)
				};

				tasks.Add (client.PublishAsync (message, QualityOfService.AtLeastOnce));
			}

			await Task.WhenAll (tasks);

			Assert.True (client.IsConnected);

			client.Close ();
		}

		[Fact]
		public async Task when_publish_messages_with_qos2_then_succeeds()
		{
			var client = GetClient ();
			var topic = Guid.NewGuid ().ToString ();
			var count = GetTestLoad();
			var tasks = new List<Task> ();

			for (var i = 1; i <= count; i++) {
				var testMessage = GetTestMessage();
				var message = new ApplicationMessage
				{
					Topic = topic,
					Payload = Serializer.Serialize(testMessage)
				};

				tasks.Add (client.PublishAsync (message, QualityOfService.ExactlyOnce));
			}

			await Task.WhenAll (tasks);

			Assert.True (client.IsConnected);

			client.Close ();
		}

		[Fact]
		public async Task when_publish_message_to_topic_then_message_is_dispatched_to_subscribers()
		{
			var count = GetTestLoad();

			var guid = Guid.NewGuid ().ToString ();
			var topicFilter = guid + "/#";
			var topic = guid;

			var publisher = GetClient ();
			var subscriber1 = GetClient ();
			var subscriber2 = GetClient ();

			var subscriber1Done = new ManualResetEventSlim ();
			var subscriber2Done = new ManualResetEventSlim ();
			var subscriber1Received = 0;
			var subscriber2Received = 0;

			await subscriber1.SubscribeAsync (topicFilter, QualityOfService.AtMostOnce)
				.ConfigureAwait(continueOnCapturedContext: false);
			await subscriber2.SubscribeAsync (topicFilter, QualityOfService.AtMostOnce)
				.ConfigureAwait(continueOnCapturedContext: false);

			subscriber1.Receiver
				.Subscribe (m => {
					if (m.Topic == topic) {
						subscriber1Received++;

						if (subscriber1Received == count)
							subscriber1Done.Set ();
					}
				});

			subscriber2.Receiver
				.Subscribe (m => {
					if (m.Topic == topic) {
						subscriber2Received++;

						if (subscriber2Received == count)
							subscriber2Done.Set ();
					}
				});

			var tasks = new List<Task> ();

			for (var i = 1; i <= count; i++) {
				var testMessage = GetTestMessage();
				var message = new ApplicationMessage
				{ 
					Topic = topic,
					Payload = Serializer.Serialize(testMessage)
				};

				tasks.Add (publisher.PublishAsync (message, QualityOfService.AtMostOnce));
			}

			await Task.WhenAll (tasks);

			var completed = WaitHandle.WaitAll (new WaitHandle[] { subscriber1Done.WaitHandle, subscriber2Done.WaitHandle }, TimeSpan.FromSeconds(Configuration.WaitingTimeoutSecs));

			Assert.Equal (count, subscriber1Received);
			Assert.Equal (count, subscriber2Received);
			Assert.True (completed);

			await subscriber1.UnsubscribeAsync (topicFilter)
				.ConfigureAwait(continueOnCapturedContext: false);
			await subscriber2.UnsubscribeAsync (topicFilter)
				.ConfigureAwait(continueOnCapturedContext: false);

			subscriber1.Close ();
			subscriber2.Close ();
			publisher.Close ();
		}

		[Fact]
		public async Task when_publish_message_to_topic_and_there_is_no_subscribers_then_server_notifies()
		{
			var count = GetTestLoad();

			var topic = Guid.NewGuid ().ToString ();
			var publisher = GetClient ();
			var topicsNotSubscribedCount = 0;
			var topicsNotSubscribedDone = new ManualResetEventSlim ();

			server.TopicNotSubscribed += (sender, e) => {
				topicsNotSubscribedCount++;

				if (topicsNotSubscribedCount == count) {
					topicsNotSubscribedDone.Set ();
				}
			};

			var tasks = new List<Task> ();

			for (var i = 1; i <= count; i++) {
				var testMessage = GetTestMessage();
				var message = new ApplicationMessage
				{ 
					Topic = topic,
					Payload = Serializer.Serialize(testMessage)
				};

				tasks.Add (publisher.PublishAsync (message, QualityOfService.AtMostOnce));
			}

			await Task.WhenAll (tasks);

			var success = topicsNotSubscribedDone.Wait (TimeSpan.FromSeconds(keepAliveSecs * 2));

			Assert.Equal (count, topicsNotSubscribedCount);
			Assert.True (success);

			publisher.Close ();
		}

		[Fact]
		public async Task when_publish_message_to_topic_and_expect_reponse_to_other_topic_then_succeeds()
		{
			var count = GetTestLoad();

			var guid = Guid.NewGuid ().ToString ();
			var requestTopic = guid;
			var responseTopic = guid + "/response";

			var publisher = GetClient ();
			var subscriber = GetClient ();

			var subscriberDone = new ManualResetEventSlim ();
			var subscriberReceived = 0;

			await subscriber.SubscribeAsync (requestTopic, QualityOfService.AtMostOnce)
				.ConfigureAwait(continueOnCapturedContext: false);
			await publisher.SubscribeAsync (responseTopic, QualityOfService.AtMostOnce)
				.ConfigureAwait(continueOnCapturedContext: false);

			subscriber.Receiver
				.Subscribe (async m => {
					if (m.Topic == requestTopic) {
						var request = Serializer.Deserialize<RequestMessage>(m.Payload);
						var response = GetResponseMessage (request);
						var message = new ApplicationMessage {
							Topic = responseTopic,
							Payload = Serializer.Serialize(response)
						};

						await subscriber.PublishAsync (message, QualityOfService.AtMostOnce)
							.ConfigureAwait(continueOnCapturedContext: false);
					}
				});

			publisher.Receiver
				.Subscribe (m => {
					if (m.Topic == responseTopic) {
						subscriberReceived++;

						if (subscriberReceived == count)
							subscriberDone.Set ();
					}
				});

			var tasks = new List<Task> ();

			for (var i = 1; i <= count; i++) {
				var request = GetRequestMessage ();
				var message = new ApplicationMessage
				{ 
					Topic = requestTopic,
					Payload = Serializer.Serialize(request)
				};

				tasks.Add (publisher.PublishAsync (message, QualityOfService.AtMostOnce));
			}

			await Task.WhenAll (tasks);

			var completed = subscriberDone.Wait (TimeSpan.FromSeconds (Configuration.WaitingTimeoutSecs));

			Assert.Equal (count, subscriberReceived);
			Assert.True (completed);

			await subscriber.UnsubscribeAsync (requestTopic)
				.ConfigureAwait(continueOnCapturedContext: false);
			await publisher.UnsubscribeAsync (responseTopic)
				.ConfigureAwait(continueOnCapturedContext: false);

			subscriber.Close ();
			publisher.Close ();
		}

		[Fact]
		public async Task when_publish_with_qos0_and_subscribe_with_same_client_intensively_then_succeeds()
		{
			var client = GetClient ();
			var count = GetTestLoad ();
			var tasks = new List<Task> ();

			for (var i = 1; i <= count; i++) {
				var subscribePublishTask = client
					.SubscribeAsync (Guid.NewGuid ().ToString (), QualityOfService.AtMostOnce)
					.ContinueWith(t => client.PublishAsync (new ApplicationMessage (Guid.NewGuid ().ToString (), Encoding.UTF8.GetBytes ("Foo Message")), QualityOfService.AtMostOnce));

				tasks.Add (subscribePublishTask);
			}

			await Task.WhenAll (tasks);

			Assert.True (client.IsConnected);
		}

		[Fact]
		public async Task when_publish_with_qos1_and_subscribe_with_same_client_intensively_then_succeeds()
		{
			var client = GetClient ();
			var count = GetTestLoad ();
			var tasks = new List<Task> ();

			for (var i = 1; i <= count; i++) {
				var subscribePublishTask = client
					.SubscribeAsync (Guid.NewGuid ().ToString (), QualityOfService.AtLeastOnce)
					.ContinueWith(t => client.PublishAsync (new ApplicationMessage (Guid.NewGuid ().ToString (), Encoding.UTF8.GetBytes ("Foo Message")), QualityOfService.AtLeastOnce));

				tasks.Add(subscribePublishTask);
			}

			await Task.WhenAll (tasks);

			Assert.True (client.IsConnected);
		}

		[Fact]
		public async Task when_publish_with_qos2_and_subscribe_with_same_client_intensively_then_succeeds()
		{
			var client = GetClient ();
			var count = GetTestLoad ();
			var tasks = new List<Task> ();

			for (var i = 1; i <= count; i++) {
				var subscribePublishTask = client
						.SubscribeAsync (Guid.NewGuid ().ToString (), QualityOfService.ExactlyOnce)
						.ContinueWith(t => client.PublishAsync (new ApplicationMessage (Guid.NewGuid ().ToString (), Encoding.UTF8.GetBytes ("Foo Message")), QualityOfService.ExactlyOnce), TaskContinuationOptions.OnlyOnRanToCompletion);

				tasks.Add(subscribePublishTask);
			}

			await Task.WhenAll (tasks);

			Assert.True (client.IsConnected);
		}

		public void Dispose ()
		{
			if (server != null) {
				server.Stop ();
			}
		}

		TestMessage GetTestMessage()
		{
			return new TestMessage {
				Name = string.Concat("Message ", Guid.NewGuid().ToString().Substring(0, 4)),
				Value = new Random().Next()
			};
		}

		RequestMessage GetRequestMessage()
		{
			return new RequestMessage {
				Id = Guid.NewGuid(),
				Name = string.Concat("Request ", Guid.NewGuid().ToString().Substring(0, 4)),
				Date = DateTime.Now,
				Content = new byte[30]
			};
		}

		ResponseMessage GetResponseMessage(RequestMessage request)
		{
			return new ResponseMessage {
				Name = request.Name,
				Ok = true
			};
		}
	}
}
