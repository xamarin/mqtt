using IntegrationTests.Context;
using IntegrationTests.Messages;
using System;
using System.Collections.Generic;
using System.Net.Mqtt;
using System.Net.Mqtt.Sdk;
using System.Net.Mqtt.Sdk.Packets;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IntegrationTests
{
	public class PublishingSpec : ConnectedContext, IDisposable
	{
		readonly IMqttServer server;

		public PublishingSpec () 
			: base(keepAliveSecs: 1)
		{
			server = GetServerAsync ().Result;
		}

		[Fact]
		public async Task when_publish_messages_with_qos0_then_succeeds()
		{
			var client = await GetClientAsync ();
			var topic = Guid.NewGuid ().ToString ();
			var count = GetTestLoad();
			var tasks = new List<Task> ();

			for (var i = 1; i <= count; i++) {
				var testMessage = GetTestMessage (i);
                var message = new MqttApplicationMessage (topic, Serializer.Serialize (testMessage));

				tasks.Add (client.PublishAsync (message, MqttQualityOfService.AtMostOnce));
			}

			await Task.WhenAll (tasks);

			Assert.True (client.IsConnected);

			client.Dispose ();
		}

		[Fact]
		public async Task when_publish_messages_with_qos1_then_succeeds()
		{
			var client = await GetClientAsync ();
			var topic = Guid.NewGuid ().ToString ();
			var count = GetTestLoad();
			var tasks = new List<Task> ();

            var publishAckPackets = 0;

            (client as MqttClientImpl)
               .Channel
               .ReceiverStream
               .Subscribe(packet => {
                   if (packet is PublishAck)
                   {
                       publishAckPackets++;
                   }
               });

            for (var i = 1; i <= count; i++) {
				var testMessage = GetTestMessage(i);
                var message = new MqttApplicationMessage (topic, Serializer.Serialize (testMessage));

				tasks.Add (client.PublishAsync (message, MqttQualityOfService.AtLeastOnce));
			}

			await Task.WhenAll (tasks);

			Assert.True (client.IsConnected);
            Assert.True (publishAckPackets >= count);

            client.Dispose ();
		}

		[Fact]
		public async Task when_publish_messages_with_qos2_then_succeeds()
		{
			var client = await GetClientAsync ();
			var topic = Guid.NewGuid ().ToString ();
			var count = GetTestLoad();
			var tasks = new List<Task> ();

            var publishReceivedPackets = 0;
            var publishCompletePackets = 0;

            (client as MqttClientImpl)
                .Channel
                .ReceiverStream
                .Subscribe(packet =>  {
                    if (packet is PublishReceived) {
                        publishReceivedPackets++;
                    } else if (packet is PublishComplete) {
                        publishCompletePackets++;
                    }
                });

			for (var i = 1; i <= count; i++) {
				var testMessage = GetTestMessage(i);
                var message = new MqttApplicationMessage (topic, Serializer.Serialize (testMessage));

				tasks.Add (client.PublishAsync (message, MqttQualityOfService.ExactlyOnce));
			}

			await Task.WhenAll (tasks);

            Assert.True (client.IsConnected);
            Assert.True (publishReceivedPackets >= count);
            Assert.True (publishCompletePackets >= count);

			client.Dispose ();
		}

		[Fact]
		public async Task when_publish_message_to_topic_then_message_is_dispatched_to_subscribers()
		{
			var count = GetTestLoad();

			var guid = Guid.NewGuid ().ToString ();
			var topicFilter = guid + "/#";
			var topic = guid;

			var publisher = await GetClientAsync ();
			var subscriber1 = await GetClientAsync ();
			var subscriber2 = await GetClientAsync ();

			var subscriber1Done = new ManualResetEventSlim ();
			var subscriber2Done = new ManualResetEventSlim ();
			var subscriber1Received = 0;
			var subscriber2Received = 0;

			await subscriber1.SubscribeAsync (topicFilter, MqttQualityOfService.AtMostOnce)
				.ConfigureAwait(continueOnCapturedContext: false);
			await subscriber2.SubscribeAsync (topicFilter, MqttQualityOfService.AtMostOnce)
				.ConfigureAwait(continueOnCapturedContext: false);

			subscriber1.MessageStream
				.Subscribe (m => {
					if (m.Topic == topic) {
						subscriber1Received++;

						if (subscriber1Received == count)
							subscriber1Done.Set ();
					}
				});

			subscriber2.MessageStream
				.Subscribe (m => {
					if (m.Topic == topic) {
						subscriber2Received++;

						if (subscriber2Received == count)
							subscriber2Done.Set ();
					}
				});

			var tasks = new List<Task> ();

			for (var i = 1; i <= count; i++) {
				var testMessage = GetTestMessage (i);
                var message = new MqttApplicationMessage (topic, Serializer.Serialize (testMessage));

				tasks.Add (publisher.PublishAsync (message, MqttQualityOfService.AtMostOnce));
			}

			await Task.WhenAll (tasks);

			var completed = WaitHandle.WaitAll (new WaitHandle[] { subscriber1Done.WaitHandle, subscriber2Done.WaitHandle }, TimeSpan.FromSeconds(Configuration.WaitTimeoutSecs));

			Assert.Equal (count, subscriber1Received);
			Assert.Equal (count, subscriber2Received);
			Assert.True (completed);

			await subscriber1.UnsubscribeAsync (topicFilter)
				.ConfigureAwait(continueOnCapturedContext: false);
			await subscriber2.UnsubscribeAsync (topicFilter)
				.ConfigureAwait(continueOnCapturedContext: false);

			subscriber1.Dispose ();
			subscriber2.Dispose ();
			publisher.Dispose ();
		}

		[Fact]
		public async Task when_publish_message_to_topic_and_there_is_no_subscribers_then_server_notifies()
		{
			var count = GetTestLoad();

			var topic = Guid.NewGuid ().ToString ();
			var publisher = await GetClientAsync ();
			var topicsNotSubscribedCount = 0;
			var topicsNotSubscribedDone = new ManualResetEventSlim ();

			server.MessageUndelivered += (sender, e) => {
				topicsNotSubscribedCount++;

				if (topicsNotSubscribedCount == count) {
					topicsNotSubscribedDone.Set ();
				}
			};

			var tasks = new List<Task> ();

			for (var i = 1; i <= count; i++) {
				var testMessage = GetTestMessage (i);
                var message = new MqttApplicationMessage (topic, Serializer.Serialize (testMessage));

				tasks.Add (publisher.PublishAsync (message, MqttQualityOfService.AtMostOnce));
			}

			await Task.WhenAll (tasks);

			var success = topicsNotSubscribedDone.Wait (TimeSpan.FromSeconds(keepAliveSecs * 2));

			Assert.Equal (count, topicsNotSubscribedCount);
			Assert.True (success);

			publisher.Dispose ();
		}

		[Fact]
		public async Task when_publish_message_to_topic_and_expect_reponse_to_other_topic_then_succeeds()
		{
			var count = GetTestLoad();

			var guid = Guid.NewGuid ().ToString ();
			var requestTopic = guid;
			var responseTopic = guid + "/response";

			var publisher = await GetClientAsync ();
			var subscriber = await GetClientAsync ();

			var subscriberDone = new ManualResetEventSlim ();
			var subscriberReceived = 0;

			await subscriber.SubscribeAsync (requestTopic, MqttQualityOfService.AtMostOnce)
				.ConfigureAwait(continueOnCapturedContext: false);
			await publisher.SubscribeAsync (responseTopic, MqttQualityOfService.AtMostOnce)
				.ConfigureAwait(continueOnCapturedContext: false);

			subscriber.MessageStream
				.Subscribe (async m => {
					if (m.Topic == requestTopic) {
						var request = Serializer.Deserialize<RequestMessage>(m.Payload);
						var response = GetResponseMessage (request);
                        var message = new MqttApplicationMessage (responseTopic, Serializer.Serialize (response));

						await subscriber.PublishAsync (message, MqttQualityOfService.AtMostOnce)
							.ConfigureAwait(continueOnCapturedContext: false);
					}
				});

			publisher.MessageStream
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
                var message = new MqttApplicationMessage (requestTopic, Serializer.Serialize (request));

				tasks.Add (publisher.PublishAsync (message, MqttQualityOfService.AtMostOnce));
			}

			await Task.WhenAll (tasks);

			var completed = subscriberDone.Wait (TimeSpan.FromSeconds (Configuration.WaitTimeoutSecs));

			Assert.Equal (count, subscriberReceived);
			Assert.True (completed);

			await subscriber.UnsubscribeAsync (requestTopic)
				.ConfigureAwait(continueOnCapturedContext: false);
			await publisher.UnsubscribeAsync (responseTopic)
				.ConfigureAwait(continueOnCapturedContext: false);

			subscriber.Dispose ();
			publisher.Dispose ();
		}

		[Fact]
		public async Task when_publish_with_qos0_and_subscribe_with_same_client_intensively_then_succeeds()
		{
			var client = await GetClientAsync ();
			var count = GetTestLoad ();
			var tasks = new List<Task> ();

			for (var i = 1; i <= count; i++) {
				var subscribePublishTask = client
					.SubscribeAsync (Guid.NewGuid ().ToString (), MqttQualityOfService.AtMostOnce)
					.ContinueWith(t => client.PublishAsync (new MqttApplicationMessage (Guid.NewGuid ().ToString (), Encoding.UTF8.GetBytes ("Foo Message")), MqttQualityOfService.AtMostOnce));

				tasks.Add (subscribePublishTask);
			}

			await Task.WhenAll (tasks);

			Assert.True (client.IsConnected);
		}

		[Fact]
		public async Task when_publish_with_qos1_and_subscribe_with_same_client_intensively_then_succeeds()
		{
			var client = await GetClientAsync ();
			var count = GetTestLoad ();
			var tasks = new List<Task> ();

			for (var i = 1; i <= count; i++) {
				var subscribePublishTask = client
					.SubscribeAsync (Guid.NewGuid ().ToString (), MqttQualityOfService.AtLeastOnce)
					.ContinueWith(t => client.PublishAsync (new MqttApplicationMessage (Guid.NewGuid ().ToString (), Encoding.UTF8.GetBytes ("Foo Message")), MqttQualityOfService.AtLeastOnce));

				tasks.Add(subscribePublishTask);
			}

			await Task.WhenAll (tasks);

			Assert.True (client.IsConnected);
		}

		[Fact]
		public async Task when_publish_with_qos2_and_subscribe_with_same_client_intensively_then_succeeds()
		{
			var client = await GetClientAsync ();
			var count = GetTestLoad ();
			var tasks = new List<Task> ();

			for (var i = 1; i <= count; i++) {
				var subscribePublishTask = client
						.SubscribeAsync (Guid.NewGuid ().ToString (), MqttQualityOfService.ExactlyOnce)
						.ContinueWith(t => client.PublishAsync (new MqttApplicationMessage (Guid.NewGuid ().ToString (), Encoding.UTF8.GetBytes ("Foo Message")), MqttQualityOfService.ExactlyOnce), TaskContinuationOptions.OnlyOnRanToCompletion);

				tasks.Add(subscribePublishTask);
			}

			await Task.WhenAll (tasks);

			Assert.True (client.IsConnected);
		}

        [Fact]
        public async Task when_publish_system_messages_then_fails_and_server_disconnects_client()
        {
            var client = await GetClientAsync ();
            var topic = "$SYS/" + Guid.NewGuid ().ToString ();
            var message = new MqttApplicationMessage (topic, Encoding.UTF8.GetBytes ("Foo Message"));

            var clientDisconnectedEvent = new ManualResetEventSlim ();

            client.Disconnected += (sender, e) => {
                if (e.Reason == DisconnectedReason.RemoteDisconnected) {
                    clientDisconnectedEvent.Set ();
                }
            };

            await client.PublishAsync (message, MqttQualityOfService.ExactlyOnce);

            var clientRemoteDisconnected = clientDisconnectedEvent.Wait (2000);

            Assert.True (clientRemoteDisconnected);

            client.Dispose();
        }

		[Fact]
		public async Task when_publish_without_clean_session_then_pending_messages_are_sent_when_reconnect()
		{
			var client1 = await GetClientAsync();
			var client1Done = new ManualResetEventSlim();
			var client1Received = 0;

			var client2 = await GetClientAsync();
			var client2Id = client2.Id;
			var client2Done = new ManualResetEventSlim();
			var client2Received = 0;

			var topic = "topic/foo/bar";
			var messagesBeforeDisconnect = 3;
			var messagesAfterReconnect = 2;

			await client1.SubscribeAsync(topic, MqttQualityOfService.AtLeastOnce);
			await client2.SubscribeAsync(topic, MqttQualityOfService.AtLeastOnce);

			var subscription1 = client1
				.MessageStream
				.Where(m => m.Topic == topic)
				.Subscribe(m => {
					client1Received++;

					if (client1Received == messagesBeforeDisconnect)
						client1Done.Set();
				});

			var subscription2 = client2
				.MessageStream
				.Where(m => m.Topic == topic)
				.Subscribe(m => {
					client2Received++;

					if (client2Received == messagesBeforeDisconnect)
						client2Done.Set();
				});

			for (var i = 1; i <= messagesBeforeDisconnect; i++) {
				var testMessage = GetTestMessage(i);
				var message = new MqttApplicationMessage(topic, Serializer.Serialize(testMessage));

				await client1.PublishAsync(message, MqttQualityOfService.AtLeastOnce, retain: false);
			}

			var completed = WaitHandle.WaitAll(new WaitHandle[] { client1Done.WaitHandle, client2Done.WaitHandle }, TimeSpan.FromSeconds(Configuration.WaitTimeoutSecs));

			Assert.True(completed, $"Messages before disconnect weren't all received. Client 1 received: {client1Received}, Client 2 received: {client2Received}");
			Assert.Equal(messagesBeforeDisconnect, client1Received);
			Assert.Equal(messagesBeforeDisconnect, client2Received);

			await client2.DisconnectAsync();

			subscription1.Dispose();
			client1Received = 0;
			client1Done.Reset();
			subscription2.Dispose();
			client2Received = 0;
			client2Done.Reset();

			var client1OldMessagesReceived = 0;
			var client2OldMessagesReceived = 0;

			subscription1 = client1
				.MessageStream
				.Where(m => m.Topic == topic)
				.Subscribe(m => {
					var testMessage = Serializer.Deserialize<TestMessage>(m.Payload);

					if (testMessage.Id > messagesBeforeDisconnect)
						client1Received++;
					else
						client1OldMessagesReceived++;

					if (client1Received == messagesAfterReconnect)
						client1Done.Set();
				});

			subscription2 = client2
				.MessageStream
				.Where(m => m.Topic == topic)
				.Subscribe(m => {
					var testMessage = Serializer.Deserialize<TestMessage>(m.Payload);

					if (testMessage.Id > messagesBeforeDisconnect)
						client2Received++;
					else
						client2OldMessagesReceived++;

					if (client2Received == messagesAfterReconnect)
						client2Done.Set();
				});

			for (var i = messagesBeforeDisconnect + 1; i <= messagesBeforeDisconnect + messagesAfterReconnect; i++) {
				var testMessage = GetTestMessage(i);
				var message = new MqttApplicationMessage(topic, Serializer.Serialize(testMessage));

				await client1.PublishAsync(message, MqttQualityOfService.AtLeastOnce, retain: false);
			}

			await client2.ConnectAsync(new MqttClientCredentials(client2Id), cleanSession: false);

			completed = WaitHandle.WaitAll(new WaitHandle[] { client1Done.WaitHandle, client2Done.WaitHandle }, TimeSpan.FromSeconds(Configuration.WaitTimeoutSecs));

			Assert.True(completed, $"Messages after re connect weren't all received. Client 1 received: {client1Received}, Client 2 received: {client2Received}");
			Assert.Equal(messagesAfterReconnect, client1Received);
			Assert.Equal(messagesAfterReconnect, client2Received);
			Assert.Equal(0, client1OldMessagesReceived);
			Assert.Equal(0, client2OldMessagesReceived);

			await client1.UnsubscribeAsync(topic)
				.ConfigureAwait(continueOnCapturedContext: false);
			await client2.UnsubscribeAsync(topic)
				.ConfigureAwait(continueOnCapturedContext: false);

			client1.Dispose();
			client2.Dispose();
		}

		[Fact]
		public async Task when_publish_with_client_with_session_present_then_subscriptions_are_re_used()
		{
			var count = GetTestLoad();
			var topic = "topic/foo/bar";

			var publisher = await GetClientAsync();
			var subscriber = await GetClientAsync();
			var subscriberId = subscriber.Id;

			var subscriberDone = new ManualResetEventSlim();
			var subscriberReceived = 0;

			await subscriber
				.SubscribeAsync(topic, MqttQualityOfService.AtMostOnce)
				.ConfigureAwait(continueOnCapturedContext: false);

			subscriber
				.MessageStream
				.Where(m => m.Topic == topic)
				.Subscribe(m => {
					subscriberReceived++;

					if (subscriberReceived == count)
						subscriberDone.Set();
				});

			await subscriber.DisconnectAsync();

			var sessionState = await subscriber.ConnectAsync(new MqttClientCredentials(subscriberId), cleanSession: false);

			subscriber
				.MessageStream
				.Where(m => m.Topic == topic)
				.Subscribe(m => {
					subscriberReceived++;

					if (subscriberReceived == count)
						subscriberDone.Set();
				});

			var tasks = new List<Task>();

			for (var i = 1; i <= count; i++)
			{
				var testMessage = GetTestMessage(i);
				var message = new MqttApplicationMessage(topic, Serializer.Serialize(testMessage));

				tasks.Add(publisher.PublishAsync(message, MqttQualityOfService.AtMostOnce));
			}

			await Task.WhenAll(tasks);

			var completed = subscriberDone.Wait(TimeSpan.FromSeconds(Configuration.WaitTimeoutSecs));

			Assert.True(completed);
			Assert.Equal(SessionState.SessionPresent, sessionState);
			Assert.Equal(count, subscriberReceived);

			await subscriber.UnsubscribeAsync(topic)
				.ConfigureAwait(continueOnCapturedContext: false);

			subscriber.Dispose();
			publisher.Dispose();
		}

		[Fact]
		public async Task when_publish_with_client_with_session_clared_then_subscriptions_are_not_re_used()
		{
			CleanSession = true;

			var count = GetTestLoad();
			var topic = "topic/foo/bar";

			var publisher = await GetClientAsync();
			var subscriber = await GetClientAsync();
			var subscriberId = subscriber.Id;

			var subscriberDone = new ManualResetEventSlim();
			var subscriberReceived = 0;

			await subscriber
				.SubscribeAsync(topic, MqttQualityOfService.AtMostOnce)
				.ConfigureAwait(continueOnCapturedContext: false);

			subscriber
				.MessageStream
				.Where(m => m.Topic == topic)
				.Subscribe(m => {
					subscriberReceived++;

					if (subscriberReceived == count)
						subscriberDone.Set();
				});

			await subscriber.DisconnectAsync();
			var sessionState = await subscriber.ConnectAsync(new MqttClientCredentials(subscriberId), cleanSession: true);

			var tasks = new List<Task>();

			for (var i = 1; i <= count; i++)
			{
				var testMessage = GetTestMessage(i);
				var message = new MqttApplicationMessage(topic, Serializer.Serialize(testMessage));

				tasks.Add(publisher.PublishAsync(message, MqttQualityOfService.AtMostOnce));
			}

			await Task.WhenAll(tasks);

			var completed = subscriberDone.Wait(TimeSpan.FromSeconds(Configuration.WaitTimeoutSecs));

			Assert.False(completed);
			Assert.Equal(SessionState.CleanSession, sessionState);
			Assert.Equal(0, subscriberReceived);

			await subscriber.UnsubscribeAsync(topic)
				.ConfigureAwait(continueOnCapturedContext: false);

			subscriber.Dispose();
			publisher.Dispose();
		}

		[Fact]
		public async Task when_publish_messages_and_client_disconnects_then_message_stream_is_reset()
		{
			var topic = Guid.NewGuid().ToString();

			var publisher = await GetClientAsync();
			var subscriber = await GetClientAsync();
			var subscriberId = subscriber.Id;

			var goal = default(int);
			var goalAchieved = new ManualResetEventSlim();
			var received = 0;

			await subscriber.SubscribeAsync(topic, MqttQualityOfService.AtMostOnce).ConfigureAwait(continueOnCapturedContext: false);

			subscriber
				.MessageStream
				.Subscribe(m => {
					if (m.Topic == topic)
					{
						received++;

						if (received == goal)
							goalAchieved.Set();
					}
				});

			goal = 5;

			var tasks = new List<Task>();

			for (var i = 1; i <= goal; i++)
			{
				var testMessage = GetTestMessage(i);
				var message = new MqttApplicationMessage(topic, Serializer.Serialize(testMessage));

				tasks.Add(publisher.PublishAsync(message, MqttQualityOfService.AtMostOnce));
			}

			await Task.WhenAll(tasks);

			var completed = goalAchieved.Wait(TimeSpan.FromSeconds(Configuration.WaitTimeoutSecs));

			Assert.True(completed);
			Assert.Equal(goal, received);

			await subscriber.DisconnectAsync();

			goal = 3;
			goalAchieved.Reset();
			received = 0;
			completed = false;

			await subscriber.ConnectAsync(new MqttClientCredentials(subscriberId), cleanSession: false);

			for (var i = 1; i <= goal; i++)
			{
				var testMessage = GetTestMessage(i);
				var message = new MqttApplicationMessage(topic, Serializer.Serialize(testMessage));

				tasks.Add(publisher.PublishAsync(message, MqttQualityOfService.AtMostOnce));
			}

			completed = goalAchieved.Wait(TimeSpan.FromSeconds(Configuration.WaitTimeoutSecs));

			Assert.False(completed);
			Assert.Equal(0, received);

			await subscriber.UnsubscribeAsync(topic).ConfigureAwait(continueOnCapturedContext: false);

			subscriber.Dispose();
			publisher.Dispose();
		}

		public void Dispose ()
		{
			if (server != null) {
				server.Stop ();
			}
		}

		TestMessage GetTestMessage(int id)
		{
			return new TestMessage {
				Id = id,
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
