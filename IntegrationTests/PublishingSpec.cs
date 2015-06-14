using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Mqtt;
using System.Net.Mqtt.Packets;
using IntegrationTests.Context;
using IntegrationTests.Messages;
using Xunit;

namespace IntegrationTests
{
	public class PublishingSpec : ConnectedContext, IDisposable
	{
		readonly Server server;

		public PublishingSpec () 
			: base(keepAliveSecs: 1)
		{
			this.server = this.GetServer ();
		}

		[Fact]
		public async Task when_publish_messages_with_qos0_then_succeeds()
		{
			var client = this.GetClient ();
			var topic = Guid.NewGuid ().ToString ();
			var count = this.GetTestLoad();

			for (var i = 1; i <= count; i++) {
				var testMessage = this.GetTestMessage();
				var message = new ApplicationMessage
				{
					Topic = topic,
					Payload = Serializer.Serialize(testMessage)
				};

				await client.PublishAsync (message, QualityOfService.AtMostOnce)
					.ConfigureAwait(continueOnCapturedContext: false);
			}

			Assert.True (client.IsConnected);

			client.Close ();
		}

		[Fact]
		public async Task when_publish_messages_with_qos1_then_succeeds()
		{
			var client = this.GetClient ();
			var topic = Guid.NewGuid ().ToString ();
			var count = this.GetTestLoad();

			for (var i = 1; i <= count; i++) {
				var testMessage = this.GetTestMessage();
				var message = new ApplicationMessage
				{
					Topic = topic,
					Payload = Serializer.Serialize(testMessage)
				};

				await client.PublishAsync (message, QualityOfService.AtLeastOnce)
					.ConfigureAwait(continueOnCapturedContext: false);
			}

			Assert.True (client.IsConnected);

			client.Close ();
		}

		[Fact]
		public async Task when_publish_messages_with_qos2_then_succeeds()
		{
			var client = this.GetClient ();
			var topic = Guid.NewGuid ().ToString ();
			var count = this.GetTestLoad();

			for (var i = 1; i <= count; i++) {
				var testMessage = this.GetTestMessage();
				var message = new ApplicationMessage
				{
					Topic = topic,
					Payload = Serializer.Serialize(testMessage)
				};

				await client.PublishAsync (message, System.Net.Mqtt.Packets.QualityOfService.ExactlyOnce)
					.ConfigureAwait(continueOnCapturedContext: false);
			}

			Assert.True (client.IsConnected);

			client.Close ();
		}

		[Fact]
		public async Task when_publish_message_to_topic_then_message_is_dispatched_to_subscribers()
		{
			var count = this.GetTestLoad();

			var guid = Guid.NewGuid ().ToString ();
			var topicFilter = guid + "/#";
			var topic = guid;

			var publisher = this.GetClient ();
			var subscriber1 = this.GetClient ();
			var subscriber2 = this.GetClient ();

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

			for (var i = 1; i <= count; i++) {
				var testMessage = this.GetTestMessage();
				var message = new ApplicationMessage
				{ 
					Topic = topic,
					Payload = Serializer.Serialize(testMessage)
				};

				await publisher.PublishAsync (message, QualityOfService.AtMostOnce)
					.ConfigureAwait(continueOnCapturedContext: false);
			}

			var completed = WaitHandle.WaitAll (new WaitHandle[] { subscriber1Done.WaitHandle, subscriber2Done.WaitHandle }, TimeSpan.FromSeconds(this.Configuration.WaitingTimeoutSecs));

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
			var count = this.GetTestLoad();

			var topic = Guid.NewGuid ().ToString ();
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

				await publisher.PublishAsync (message, QualityOfService.AtMostOnce)
					.ConfigureAwait(continueOnCapturedContext: false);
			}

			var success = topicsNotSubscribedDone.Wait (TimeSpan.FromSeconds(this.keepAliveSecs * 2));

			Assert.Equal (count, topicsNotSubscribedCount);
			Assert.True (success);

			publisher.Close ();
		}

		[Fact]
		public async Task when_publish_message_to_topic_and_expect_reponse_to_other_topic_then_succeeds()
		{
			var count = this.GetTestLoad();

			var guid = Guid.NewGuid ().ToString ();
			var requestTopic = guid;
			var responseTopic = guid + "/response";

			var publisher = this.GetClient ();
			var subscriber = this.GetClient ();

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
						var response = this.GetResponseMessage (request);
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

			for (var i = 1; i <= count; i++) {
				var request = this.GetRequestMessage ();
				var message = new ApplicationMessage
				{ 
					Topic = requestTopic,
					Payload = Serializer.Serialize(request)
				};

				await publisher.PublishAsync (message, QualityOfService.AtMostOnce)
					.ConfigureAwait(continueOnCapturedContext: false);
			}

			var completed = subscriberDone.Wait (TimeSpan.FromSeconds (this.Configuration.WaitingTimeoutSecs));

			Assert.Equal (count, subscriberReceived);
			Assert.True (completed);

			await subscriber.UnsubscribeAsync (requestTopic)
				.ConfigureAwait(continueOnCapturedContext: false);
			await publisher.UnsubscribeAsync (responseTopic)
				.ConfigureAwait(continueOnCapturedContext: false);

			subscriber.Close ();
			publisher.Close ();
		}

		public void Dispose ()
		{
			if (this.server != null) {
				this.server.Stop ();
			}
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
