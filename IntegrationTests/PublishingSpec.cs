using System;
using System.Threading;
using System.Threading.Tasks;
using Hermes;
using Hermes.Packets;
using IntegrationTests.Context;
using IntegrationTests.Messages;
using Xunit;

namespace IntegrationTests
{
	public class PublishingSpec : ConnectedContext
	{
		public PublishingSpec () : base(keepAliveSecs: 2)
		{
		}

		[Fact]
		public async Task when_publish_messages_with_qos0_then_succeeds()
		{
			var client = this.GetClient ();
			var topic = "foo/test/qos0";
			var count = 50;

			for (var i = 0; i < count; i++) {
				var testMessage = this.GetTestMessage();
				var message = new ApplicationMessage
				{
					Topic = topic,
					Payload = Serializer.Serialize(testMessage)
				};

				await client.PublishAsync (message, Hermes.Packets.QualityOfService.AtMostOnce);
			}

			Assert.True (client.IsConnected);
		}

		[Fact]
		public async Task when_publish_messages_with_qos1_then_succeeds()
		{
			var client = this.GetClient ();
			var topic = "foo/test/qos1";
			var count = 50;

			for (var i = 0; i < count; i++) {
				var testMessage = this.GetTestMessage();
				var message = new ApplicationMessage
				{
					Topic = topic,
					Payload = Serializer.Serialize(testMessage)
				};

				await client.PublishAsync (message, Hermes.Packets.QualityOfService.AtLeastOnce);
			}

			Assert.True (client.IsConnected);
		}

		//[Fact]
		//public async Task when_publish_messages_with_qos2_then_succeeds()
		//{
		//	var client = this.GetClient ();
		//	var topic = "foo/test/qos2";
		//	var count = 50;

		//	for (var i = 0; i < count; i++) {
		//		var testMessage = this.GetTestMessage();
		//		var message = new ApplicationMessage
		//		{
		//			Topic = topic,
		//			Payload = Serializer.Serialize(testMessage)
		//		};

		//		await client.PublishAsync (message, Hermes.Packets.QualityOfService.ExactlyOnce);
		//	}

		//	Assert.True (client.IsConnected);
		//}

		[Fact]
		public async Task when_publish_message_to_topic_then_message_is_dispatched_to_subscribers()
		{
			var count = 50;

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

			subscriber1.Receiver.Subscribe (m => {
				if (m.Topic == topic) {
					subscriber1Received++;

					if (subscriber1Received == count)
						subscriber1Done.Set ();
				}
			});
			subscriber2.Receiver.Subscribe (m => {
				if (m.Topic == topic) {
					subscriber2Received++;

					if (subscriber2Received == count)
						subscriber2Done.Set ();
				}
			});

			for (var i = 0; i < count; i++) {
				var testMessage = this.GetTestMessage();
				var message = new ApplicationMessage
				{ 
					Topic = topic,
					Payload = Serializer.Serialize(testMessage)
				};

				await publisher.PublishAsync (message, QualityOfService.AtMostOnce);
			}

			var completed = WaitHandle.WaitAll (new WaitHandle[] { subscriber1Done.WaitHandle, subscriber2Done.WaitHandle }, TimeSpan.FromSeconds(this.fixture.Configuration.WaitingTimeoutSecs));

			Assert.True (completed);
		}

		[Fact]
		public async Task when_publish_message_to_topic_and_expect_reponse_to_other_topic_then_succeeds()
		{
			var count = 50;

			var requestTopic = "test/foo";
			var responseTopic = "test/foo/response";

			var publisher = this.GetClient ();
			var subscriber = this.GetClient ();

			var subscriberDone = new ManualResetEventSlim ();
			var subscriberReceived = 0;

			await subscriber.SubscribeAsync (requestTopic, QualityOfService.AtMostOnce);
			await publisher.SubscribeAsync (responseTopic, QualityOfService.AtMostOnce);

			subscriber.Receiver.Subscribe (async m => {
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

			publisher.Receiver.Subscribe (m => {
				if (m.Topic == responseTopic) {
					subscriberReceived++;

					if (subscriberReceived == count)
						subscriberDone.Set ();
				}
			});

			for (var i = 0; i < count; i++) {
				var request = this.GetRequestMessage ();
				var message = new ApplicationMessage
				{ 
					Topic = requestTopic,
					Payload = Serializer.Serialize(request)
				};

				await publisher.PublishAsync (message, QualityOfService.AtMostOnce);
			}

			var completed = subscriberDone.Wait (TimeSpan.FromSeconds (this.fixture.Configuration.WaitingTimeoutSecs));
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
