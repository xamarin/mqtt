using System;
using System.Threading;
using System.Threading.Tasks;
using Hermes;
using Hermes.Packets;
using IntegrationTests.Context;
using IntegrationTests.Messages;
using Xunit;
using System.Collections.Generic;

namespace IntegrationTests
{
	public class PublishingSpec : ConnectedContext, IDisposable
	{
		private readonly Server server;

		public PublishingSpec () 
			: base(keepAliveSecs: 2)
		{
			this.server = this.GetServer ();
		}

		[Fact]
		public async Task when_publish_messages_with_qos0_then_succeeds()
		{
			var client = this.GetClient ();
			var topic = "foo/test/qos0";
			var count = this.GetTestLoad();

			for (var i = 1; i <= count; i++) {
				var testMessage = this.GetTestMessage();
				var message = new ApplicationMessage
				{
					Topic = topic,
					Payload = Serializer.Serialize(testMessage)
				};

				await client.PublishAsync (message, QualityOfService.AtMostOnce);
			}

			Assert.True (client.IsConnected);

			client.Close ();
		}

		[Fact]
		public async Task when_publish_messages_with_qos1_then_succeeds()
		{
			var client = this.GetClient ();
			var topic = "foo/test/qos1";
			var count = this.GetTestLoad();
			var publishTasks = new List<Task> ();

			for (var i = 1; i <= count; i++) {
				var testMessage = this.GetTestMessage();
				var message = new ApplicationMessage
				{
					Topic = topic,
					Payload = Serializer.Serialize(testMessage)
				};

				await client.PublishAsync (message, QualityOfService.AtLeastOnce);
			}

			Assert.True (client.IsConnected);

			client.Close ();
		}

		[Fact]
		public async Task when_publish_messages_with_qos2_then_succeeds()
		{
			var client = this.GetClient ();
			var topic = "foo/test/qos2";
			var count = this.GetTestLoad();

			for (var i = 1; i <= count; i++) {
				var testMessage = this.GetTestMessage();
				var message = new ApplicationMessage
				{
					Topic = topic,
					Payload = Serializer.Serialize(testMessage)
				};

				await client.PublishAsync (message, Hermes.Packets.QualityOfService.ExactlyOnce);
			}

			Assert.True (client.IsConnected);

			client.Close ();
		}

		[Fact]
		public async Task when_publish_message_to_topic_then_message_is_dispatched_to_subscribers()
		{
			var count = this.GetTestLoad();

			var topicFilter = "test/#";
			var topic = "test/foo/bar";

			var publisher = this.GetClient ();
			var subscriber1 = this.GetClient ();
			var subscriber2 = this.GetClient ();

			var subscriber1Done = new ManualResetEventSlim ();
			var subscriber2Done = new ManualResetEventSlim ();
			var subscriber1Received = 0;
			var subscriber2Received = 0;

			await subscriber1.SubscribeAsync (topicFilter, QualityOfService.AtMostOnce);
			await subscriber2.SubscribeAsync (topicFilter, QualityOfService.AtMostOnce);

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

			for (var i = 1; i <= count; i++) {
				var testMessage = this.GetTestMessage();
				var message = new ApplicationMessage
				{ 
					Topic = topic,
					Payload = Serializer.Serialize(testMessage)
				};

				await publisher.PublishAsync (message, QualityOfService.AtMostOnce);
			}

			var completed = WaitHandle.WaitAll (new WaitHandle[] { subscriber1Done.WaitHandle, subscriber2Done.WaitHandle }, TimeSpan.FromSeconds(this.Configuration.WaitingTimeoutSecs));

			Assert.Equal (count, subscriber1Received);
			Assert.Equal (count, subscriber2Received);
			Assert.True (completed);

			subscriber1.Close ();
			subscriber2.Close ();
			publisher.Close ();
		}

		[Fact]
		public async Task when_publish_message_to_topic_and_there_is_no_subscribers_then_server_notifies()
		{
			var count = this.GetTestLoad();

			var topic = "test/foo/nosubscribers";
			var publisher = this.GetClient ();
			var topicsNotSubscribedCount = 0;
			var topicsNotSubscribedDone = new ManualResetEventSlim ();

			server.TopicNotSubscribed += (sender, e) => {
				topicsNotSubscribedCount++;

				if (topicsNotSubscribedCount == count) {
					topicsNotSubscribedDone.Set ();
				}
			};

			for (var i = 1; i <= count; i++) {
				var testMessage = this.GetTestMessage();
				var message = new ApplicationMessage
				{ 
					Topic = topic,
					Payload = Serializer.Serialize(testMessage)
				};

				await publisher.PublishAsync (message, QualityOfService.AtMostOnce);
			}

			var success = topicsNotSubscribedDone.Wait (TimeSpan.FromSeconds(this.keepAliveSecs));

			publisher.Close ();

			Assert.Equal (count, topicsNotSubscribedCount);
			Assert.True (success);
		}

		[Fact]
		public async Task when_publish_message_to_topic_and_expect_reponse_to_other_topic_then_succeeds()
		{
			var count = this.GetTestLoad();

			var requestTopic = "test/foo";
			var responseTopic = "test/foo/response";

			var publisher = this.GetClient ();
			var subscriber = this.GetClient ();

			var subscriberDone = new ManualResetEventSlim ();
			var subscriberReceived = 0;

			await subscriber.SubscribeAsync (requestTopic, QualityOfService.AtMostOnce);
			await publisher.SubscribeAsync (responseTopic, QualityOfService.AtMostOnce);

			subscriber.Receiver
				.Subscribe (async m => {
					if (m.Topic == requestTopic) {
						var request = Serializer.Deserialize<RequestMessage>(m.Payload);
						var response = this.GetResponseMessage (request);
						var message = new ApplicationMessage {
							Topic = responseTopic,
							Payload = Serializer.Serialize(response)
						};

						await subscriber.PublishAsync (message, QualityOfService.AtMostOnce);
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

			for (var i = 1; i <= count; i++) {
				var request = this.GetRequestMessage ();
				var message = new ApplicationMessage
				{ 
					Topic = requestTopic,
					Payload = Serializer.Serialize(request)
				};

				await publisher.PublishAsync (message, QualityOfService.AtMostOnce);
			}

			var completed = subscriberDone.Wait (TimeSpan.FromSeconds (this.Configuration.WaitingTimeoutSecs));

			Assert.Equal (count, subscriberReceived);
			Assert.True (completed);
		}

		public void Dispose ()
		{
			this.server.Stop ();
		}

		private TestMessage GetTestMessage()
		{
			return new TestMessage {
				Name = string.Concat("Message ", Guid.NewGuid().ToString().Substring(0, 4)),
				Value = new Random().Next()
			};
		}

		private RequestMessage GetRequestMessage()
		{
			return new RequestMessage {
				Id = Guid.NewGuid(),
				Name = string.Concat("Request ", Guid.NewGuid().ToString().Substring(0, 4)),
				Date = DateTime.Now,
				Content = new byte[30]
			};
		}

		private ResponseMessage GetResponseMessage(RequestMessage request)
		{
			return new ResponseMessage {
				Name = request.Name,
				Ok = true
			};
		}
	}
}
