using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mqtt;
using System.Net.Mqtt.Sdk;
using System.Net.Mqtt.Sdk.Flows;
using System.Net.Mqtt.Sdk.Packets;
using System.Net.Mqtt.Sdk.Storage;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Flows
{
	public class SubscribeFlowSpec
	{
		[Fact]
		public async Task when_subscribing_new_topics_then_subscriptions_are_created_and_ack_is_sent()
		{
			var configuration = new MqttConfiguration { MaximumQualityOfService = MqttQualityOfService.AtLeastOnce };
			var topicEvaluator = new Mock<IMqttTopicEvaluator> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();
			var packetIdProvider = Mock.Of<IPacketIdProvider> ();
			var retainedMessageRepository = Mock.Of<IRepository<RetainedMessage>> ();
			var senderFlow = Mock.Of<IPublishSenderFlow> ();

			var clientId = Guid.NewGuid().ToString();
			var session = new ClientSession (clientId, clean: false );

			topicEvaluator.Setup (e => e.IsValidTopicFilter (It.IsAny<string> ())).Returns (true);
			sessionRepository.Setup (r => r.Read (It.IsAny<string> ())).Returns (session);

			var fooQoS = MqttQualityOfService.AtLeastOnce;
			var fooTopic = "test/foo/1";
			var fooSubscription = new Subscription (fooTopic, fooQoS);
			var barQoS = MqttQualityOfService.AtMostOnce;
			var barTopic = "test/bar/1";
			var barSubscription = new Subscription (barTopic, barQoS);

			var packetId = (ushort)new Random ().Next (0, ushort.MaxValue);
			var subscribe = new Subscribe (packetId, fooSubscription, barSubscription);
			
			var channel = new Mock<IMqttChannel<IPacket>> ();

			var response = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (p => response = p)
				.Returns(Task.Delay(0));

			var connectionProvider = new Mock<IConnectionProvider> ();

			connectionProvider
				.Setup (p => p.GetConnectionAsync (It.Is<string> (c => c == clientId)))
				.Returns (Task.FromResult(channel.Object));

			var flow = new ServerSubscribeFlow (topicEvaluator.Object, sessionRepository.Object, 
				retainedMessageRepository, packetIdProvider, senderFlow, configuration);

			await flow.ExecuteAsync (clientId, subscribe, channel.Object)
				.ConfigureAwait(continueOnCapturedContext: false);

			sessionRepository.Verify (r => r.Update (It.Is<ClientSession> (s => s.Id == clientId && s.Subscriptions.Count == 2 
				&& s.Subscriptions.All(x => x.TopicFilter == fooTopic || x.TopicFilter == barTopic))));
			Assert.NotNull (response);

			var subscribeAck = response as SubscribeAck;

			Assert.NotNull (subscribeAck);
			Assert.Equal (packetId, subscribeAck.PacketId);
			Assert.Equal (2, subscribeAck.ReturnCodes.Count ());
			Assert.Contains (subscribeAck.ReturnCodes, c => c == SubscribeReturnCode.MaximumQoS0);
			Assert.Contains (subscribeAck.ReturnCodes, c => c == SubscribeReturnCode.MaximumQoS1);
		}

		[Fact]
		public async Task when_subscribing_existing_topics_then_subscriptions_are_updated_and_ack_is_sent()
		{
			var configuration = new MqttConfiguration { MaximumQualityOfService = MqttQualityOfService.AtLeastOnce };
			var topicEvaluator = new Mock<IMqttTopicEvaluator> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();
			var packetIdProvider = Mock.Of<IPacketIdProvider> ();
			var retainedMessageRepository = Mock.Of<IRepository<RetainedMessage>> ();
			var senderFlow = Mock.Of<IPublishSenderFlow> ();

			var clientId = Guid.NewGuid().ToString();
			var fooQoS = MqttQualityOfService.AtLeastOnce;
			var fooTopic = "test/foo/1";
			var fooSubscription = new Subscription (fooTopic, fooQoS);

			var session = new ClientSession (clientId, clean: false) {  
				Subscriptions = new List<ClientSubscription> { 
					new ClientSubscription { ClientId = clientId, MaximumQualityOfService = MqttQualityOfService.ExactlyOnce, TopicFilter = fooTopic } 
				} 
			};

			topicEvaluator.Setup (e => e.IsValidTopicFilter (It.IsAny<string> ())).Returns (true);
			sessionRepository.Setup (r => r.Read (It.IsAny<string> ())).Returns (session);

			var packetId = (ushort)new Random ().Next (0, ushort.MaxValue);
			var subscribe = new Subscribe (packetId, fooSubscription);
			
			var channel = new Mock<IMqttChannel<IPacket>> ();

			var response = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (p => response = p)
				.Returns(Task.Delay(0));

			var connectionProvider = new Mock<IConnectionProvider> ();

			connectionProvider
				.Setup (p => p.GetConnectionAsync (It.Is<string> (c => c == clientId)))
				.Returns (Task.FromResult(channel.Object));

			var flow = new ServerSubscribeFlow (topicEvaluator.Object,  sessionRepository.Object, 
				retainedMessageRepository, packetIdProvider,
				senderFlow, configuration);

			await flow.ExecuteAsync (clientId, subscribe, channel.Object)
				.ConfigureAwait(continueOnCapturedContext: false);

			sessionRepository.Verify (r => r.Update (It.Is<ClientSession> (s => s.Id == clientId && s.Subscriptions.Count == 1 
				&& s.Subscriptions.Any(x => x.TopicFilter == fooTopic && x.MaximumQualityOfService == fooQoS))));
			Assert.NotNull (response);

			var subscribeAck = response as SubscribeAck;

			Assert.NotNull (subscribeAck);
			Assert.Equal (packetId, subscribeAck.PacketId);
			Assert.Single (subscribeAck.ReturnCodes);
			Assert.Contains (subscribeAck.ReturnCodes, c => c == SubscribeReturnCode.MaximumQoS1);
		}

		[Fact]
		public async Task when_subscribing_invalid_topic_then_failure_is_sent_in_ack()
		{
			var configuration = new MqttConfiguration { MaximumQualityOfService = MqttQualityOfService.AtLeastOnce };
			var topicEvaluator = new Mock<IMqttTopicEvaluator> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();
			var packetIdProvider = Mock.Of<IPacketIdProvider> ();
			var retainedMessageRepository = Mock.Of<IRepository<RetainedMessage>> ();
			var senderFlow = Mock.Of<IPublishSenderFlow> ();

			var clientId = Guid.NewGuid().ToString();
			var session = new ClientSession (clientId, clean: false );

			topicEvaluator.Setup (e => e.IsValidTopicFilter (It.IsAny<string> ())).Returns (false);
			sessionRepository.Setup (r => r.Read (It.IsAny<string> ())).Returns (session);

			var fooQoS = MqttQualityOfService.AtLeastOnce;
			var fooTopic = "test/foo/1";
			var fooSubscription = new Subscription (fooTopic, fooQoS);

			var packetId = (ushort)new Random ().Next (0, ushort.MaxValue);
			var subscribe = new Subscribe (packetId, fooSubscription);
			
			var channel = new Mock<IMqttChannel<IPacket>> ();

			var response = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (p => response = p)
				.Returns(Task.Delay(0));

			var connectionProvider = new Mock<IConnectionProvider> ();

			connectionProvider
				.Setup (p => p.GetConnectionAsync (It.Is<string> (c => c == clientId)))
				.Returns (Task.FromResult(channel.Object));

			var flow = new ServerSubscribeFlow (topicEvaluator.Object, sessionRepository.Object, 
				retainedMessageRepository, packetIdProvider,
				senderFlow, configuration);

			await flow.ExecuteAsync (clientId, subscribe, channel.Object)
				.ConfigureAwait(continueOnCapturedContext: false);

			Assert.NotNull (response);

			var subscribeAck = response as SubscribeAck;

			Assert.NotNull (subscribeAck);
			Assert.Equal (packetId, subscribeAck.PacketId);
			Assert.Single (subscribeAck.ReturnCodes);
			Assert.Equal(SubscribeReturnCode.Failure, subscribeAck.ReturnCodes.First());
		}

		[Fact]
		public async Task when_subscribing_topic_with_retain_message_then_retained_is_sent()
		{
			var configuration = new MqttConfiguration { MaximumQualityOfService = MqttQualityOfService.AtLeastOnce };
			var topicEvaluator = new Mock<IMqttTopicEvaluator> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();
			var packetIdProvider = Mock.Of<IPacketIdProvider> ();
			var retainedMessageRepository = new Mock<IRepository<RetainedMessage>> ();
			var senderFlow = new Mock<IPublishSenderFlow> ();

			var clientId = Guid.NewGuid().ToString();
			var session = new ClientSession (clientId, clean: false);

			sessionRepository.Setup (r => r.Read (It.IsAny<string> ())).Returns (session);

			var fooQoS = MqttQualityOfService.AtLeastOnce;
			var fooTopic = "test/foo/#";
			var fooSubscription = new Subscription (fooTopic, fooQoS);

			var retainedTopic = "test/foo/bar";
			var retainedQoS =  MqttQualityOfService.ExactlyOnce;
			var retainedPayload = Encoding.UTF8.GetBytes ("Retained Message Test");
			var retainedMessages = new List<RetainedMessage> {
				new RetainedMessage (retainedTopic, retainedQoS, retainedPayload)
			};

			topicEvaluator.Setup (e => e.IsValidTopicFilter (It.IsAny<string> ())).Returns (true);
			topicEvaluator.Setup (e => e.Matches (It.IsAny<string> (), It.IsAny<string> ())).Returns (true);
			retainedMessageRepository.Setup (r => r.ReadAll ()).Returns (retainedMessages.AsQueryable());

			var packetId = (ushort)new Random ().Next (0, ushort.MaxValue);
			var subscribe = new Subscribe (packetId, fooSubscription);
			
			var channel = new Mock<IMqttChannel<IPacket>> ();

			var connectionProvider = new Mock<IConnectionProvider> ();

			connectionProvider
				.Setup (p => p.GetConnectionAsync (It.Is<string> (c => c == clientId)))
				.Returns (Task.FromResult(channel.Object));

			var flow = new ServerSubscribeFlow (topicEvaluator.Object, 
				sessionRepository.Object, retainedMessageRepository.Object,
				packetIdProvider, senderFlow.Object, configuration);

			await flow.ExecuteAsync (clientId, subscribe, channel.Object)
				.ConfigureAwait(continueOnCapturedContext: false);

			senderFlow.Verify (f => f.SendPublishAsync (It.Is<string>(s => s == clientId),
				It.Is<Publish> (p => p.Topic == retainedTopic && 
					p.QualityOfService == fooQoS && 
					p.Payload.ToList().SequenceEqual(retainedPayload) && 
					p.PacketId.HasValue && 
					p.Retain), 
				It.Is<IMqttChannel<IPacket>>(c => c == channel.Object),
				It.Is<PendingMessageStatus>(x => x == PendingMessageStatus.PendingToSend)));
		}
	}
}
