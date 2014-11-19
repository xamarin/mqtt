using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Hermes;
using Hermes.Flows;
using Hermes.Packets;
using Hermes.Storage;
using Moq;
using Xunit;

namespace Tests.Flows
{
	public class PublishFlowSpec
	{
		[Fact]
		public async Task when_sending_publish_with_qos0_then_publish_is_sent_to_subscribers_and_no_ack_is_sent()
		{
			var configuration = new Mock<IProtocolConfiguration> ();
			var clientManager = new Mock<IClientManager> ();
			var topicEvaluator = new Mock<ITopicEvaluator> ();
			var retainedRepository = new Mock<IRepository<RetainedMessage>> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();
			var packetIdentifierRepository = Mock.Of<IRepository<PacketIdentifier>> ();

			var topic = "foo/bar";

			var flow = new PublishFlow (configuration.Object, clientManager.Object, topicEvaluator.Object, retainedRepository.Object, 
				sessionRepository.Object, packetIdentifierRepository);

			var subscribedClientId = Guid.NewGuid().ToString();
			var requestedQoS = QualityOfService.AtLeastOnce;
			var sessions = new List<ClientSession> { new ClientSession {
				ClientId = subscribedClientId,
				Clean = false,
				Subscriptions = new List<ClientSubscription> { 
					new ClientSubscription { ClientId = subscribedClientId, MaximumQualityOfService = requestedQoS, TopicFilter = topic }
				}
			}};

			topicEvaluator.Setup (e => e.Matches (It.IsAny<string> (), It.IsAny<string> ())).Returns (true);
			sessionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSession, bool>>>())).Returns (sessions.AsQueryable());
			configuration.Setup (c => c.MaximumQualityOfService).Returns (QualityOfService.AtLeastOnce);

			var clientId = Guid.NewGuid ().ToString ();
			var publish = new Publish (topic, QualityOfService.AtMostOnce, retain: false, duplicatedDelivery: false);

			publish.Payload = Encoding.UTF8.GetBytes ("Publish Flow Test");

			var channel = new Mock<IChannel<IPacket>> ();

			await flow.ExecuteAsync (clientId, publish, channel.Object);

			retainedRepository.Verify (r => r.Create (It.IsAny<RetainedMessage> ()), Times.Never);
			clientManager.Verify (m => m.SendMessageAsync (It.Is<string> (s => s == subscribedClientId),
				It.Is<IPacket> (p => p is Publish && ((Publish)p).Topic == topic && ((Publish)p).QualityOfService == requestedQoS && ((Publish)p).PacketId.HasValue)));
			channel.Verify (c => c.SendAsync (It.IsAny<IPacket> ()), Times.Never);
		}

		[Fact]
		public async Task when_sending_publish_with_qos1_then_publish_is_sent_to_subscribers_and_publish_ack_is_sent()
		{
			var configuration = new Mock<IProtocolConfiguration> ();
			var clientManager = new Mock<IClientManager> ();
			var topicEvaluator = new Mock<ITopicEvaluator> ();
			var retainedRepository = new Mock<IRepository<RetainedMessage>> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();
			var packetIdentifierRepository = Mock.Of<IRepository<PacketIdentifier>> ();

			var topic = "foo/bar";

			var flow = new PublishFlow (configuration.Object, clientManager.Object, topicEvaluator.Object, retainedRepository.Object,
				sessionRepository.Object, packetIdentifierRepository);

			var subscribedClientId = Guid.NewGuid().ToString();
			var requestedQoS = QualityOfService.ExactlyOnce;
			var sessions = new List<ClientSession> { new ClientSession {
				ClientId = subscribedClientId,
				Clean = false,
				Subscriptions = new List<ClientSubscription> { 
					new ClientSubscription { ClientId = subscribedClientId, MaximumQualityOfService = requestedQoS, TopicFilter = topic }
				}
			}};

			topicEvaluator.Setup (e => e.Matches (It.IsAny<string> (), It.IsAny<string> ())).Returns (true);
			sessionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSession, bool>>>())).Returns ( sessions.AsQueryable());
			configuration.Setup (c => c.MaximumQualityOfService).Returns (QualityOfService.ExactlyOnce);

			var clientId = Guid.NewGuid ().ToString ();
			var packetId = (ushort?)new Random ().Next (0, ushort.MaxValue);
			var publish = new Publish (topic, QualityOfService.AtLeastOnce, retain: false, duplicatedDelivery: false, packetId: packetId);

			publish.Payload = Encoding.UTF8.GetBytes ("Publish Flow Test");

			var channel = new Mock<IChannel<IPacket>> ();

			var response = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (p => response = p)
				.Returns(Task.Delay(0));

			await flow.ExecuteAsync (clientId, publish, channel.Object);

			retainedRepository.Verify (r => r.Create (It.IsAny<RetainedMessage> ()), Times.Never);
			clientManager.Verify (m => m.SendMessageAsync (It.Is<string> (s => s == subscribedClientId),
				It.Is<IPacket> (p => p is Publish && ((Publish)p).Topic == topic && ((Publish)p).QualityOfService == requestedQoS && ((Publish)p).PacketId.HasValue)));
			Assert.NotNull (response);

			var publishAck = response as PublishAck;

			Assert.NotNull (publishAck);
			Assert.Equal (packetId.Value, publishAck.PacketId);
		}

		[Fact]
		public async Task when_sending_publish_with_qos2_part1_then_publish_is_sent_to_subscribers_and_publish_received_is_sent()
		{
			var configuration = new Mock<IProtocolConfiguration> ();
			var clientManager = new Mock<IClientManager> ();
			var topicEvaluator = new Mock<ITopicEvaluator> ();
			var retainedRepository = new Mock<IRepository<RetainedMessage>> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();
			var packetIdentifierRepository = Mock.Of<IRepository<PacketIdentifier>> ();

			var topic = "foo/bar";

			var flow = new PublishFlow (configuration.Object, clientManager.Object, topicEvaluator.Object, retainedRepository.Object, 
				sessionRepository.Object, packetIdentifierRepository);

			var subscribedClientId = Guid.NewGuid().ToString();
			var requestedQoS = QualityOfService.ExactlyOnce;
			var sessions = new List<ClientSession> { new ClientSession {
				ClientId = subscribedClientId,
				Clean = false,
				Subscriptions = new List<ClientSubscription> { 
					new ClientSubscription { ClientId = subscribedClientId, MaximumQualityOfService = requestedQoS, TopicFilter = topic }
				}
			}};

			topicEvaluator.Setup (e => e.Matches (It.IsAny<string> (), It.IsAny<string> ())).Returns (true);
			sessionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSession, bool>>>())).Returns ( sessions.AsQueryable());
			configuration.Setup (c => c.MaximumQualityOfService).Returns (QualityOfService.ExactlyOnce);

			var clientId = Guid.NewGuid ().ToString ();
			var packetId = (ushort?)new Random ().Next (0, ushort.MaxValue);
			var publish = new Publish (topic, QualityOfService.ExactlyOnce, retain: false, duplicatedDelivery: false, packetId: packetId);

			publish.Payload = Encoding.UTF8.GetBytes ("Publish Flow Test");

			var channel = new Mock<IChannel<IPacket>> ();

			var response = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (p => response = p)
				.Returns(Task.Delay(0));

			await flow.ExecuteAsync (clientId, publish, channel.Object);

			clientManager.Verify (m => m.SendMessageAsync (It.Is<string> (s => s == subscribedClientId),
				It.Is<IPacket> (p => p is Publish && ((Publish)p).Topic == topic && ((Publish)p).QualityOfService == requestedQoS && ((Publish)p).PacketId.HasValue)));
			retainedRepository.Verify (r => r.Create (It.IsAny<RetainedMessage> ()), Times.Never);
			Assert.NotNull (response);

			var publishAck = response as PublishReceived;

			Assert.NotNull (publishAck);
			Assert.Equal (packetId.Value, publishAck.PacketId);
		}

		[Fact]
		public async Task when_sending_publish_with_qos2_part2_then_publish_release_is_sent()
		{
			var configuration = Mock.Of<IProtocolConfiguration> ();
			var clientManager = Mock.Of<IClientManager> ();
			var topicEvaluator = new Mock<ITopicEvaluator> ();
			var retainedRepository = Mock.Of<IRepository<RetainedMessage>> ();
			var sessionRepository = Mock.Of<IRepository<ClientSession>> ();
			var packetIdentifierRepository = Mock.Of<IRepository<PacketIdentifier>> ();

			var flow = new PublishFlow (configuration, clientManager, topicEvaluator.Object, retainedRepository, 
				sessionRepository, packetIdentifierRepository);

			var clientId = Guid.NewGuid ().ToString ();
			var packetId = (ushort)new Random ().Next (0, ushort.MaxValue);
			var publishReceived = new PublishReceived (packetId);

			var channel = new Mock<IChannel<IPacket>> ();

			var response = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (p => response = p)
				.Returns(Task.Delay(0));

			await flow.ExecuteAsync (clientId, publishReceived, channel.Object);

			Assert.NotNull (response);

			var publishRelease = response as PublishRelease;

			Assert.NotNull (publishRelease);
			Assert.Equal (packetId, publishRelease.PacketId);
		}

		[Fact]
		public async Task when_sending_publish_with_qos2_part3_then_publish_complete_is_sent()
		{
			var configuration = Mock.Of<IProtocolConfiguration> ();
			var clientManager = Mock.Of<IClientManager> ();
			var topicEvaluator = new Mock<ITopicEvaluator> ();
			var retainedRepository = Mock.Of<IRepository<RetainedMessage>> ();
			var sessionRepository = Mock.Of<IRepository<ClientSession>> ();
			var packetIdentifierRepository = Mock.Of<IRepository<PacketIdentifier>> ();

			var flow = new PublishFlow (configuration, clientManager, topicEvaluator.Object, retainedRepository, 
				sessionRepository, packetIdentifierRepository);

			var clientId = Guid.NewGuid ().ToString ();
			var packetId = (ushort)new Random ().Next (0, ushort.MaxValue);
			var publishRelease = new PublishRelease (packetId);

			var channel = new Mock<IChannel<IPacket>> ();

			var response = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (p => response = p)
				.Returns(Task.Delay(0));

			await flow.ExecuteAsync (clientId, publishRelease, channel.Object);

			Assert.NotNull (response);

			var publishComplete = response as PublishComplete;

			Assert.NotNull (publishComplete);
			Assert.Equal (packetId, publishComplete.PacketId);
		}

		[Fact]
		public async Task when_sending_publish_with_retain_then_retain_message_is_created()
		{
			var configuration = Mock.Of<IProtocolConfiguration> ();
			var clientManager = Mock.Of<IClientManager> ();
			var topicEvaluator = new Mock<ITopicEvaluator> ();
			var retainedRepository = new Mock<IRepository<RetainedMessage>> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();
			var packetIdentifierRepository = Mock.Of<IRepository<PacketIdentifier>> ();

			var topic = "foo/bar";

			var flow = new PublishFlow (configuration, clientManager, topicEvaluator.Object, retainedRepository.Object, 
				sessionRepository.Object, packetIdentifierRepository);
			var sessions = new List<ClientSession> { new ClientSession { ClientId = Guid.NewGuid ().ToString (), Clean = false }};

			retainedRepository.Setup (r => r.Get (It.IsAny<Expression<Func<RetainedMessage, bool>>>())).Returns (default(RetainedMessage));
			sessionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSession, bool>>>())).Returns ( sessions.AsQueryable());

			var clientId = Guid.NewGuid ().ToString ();
			var qos = QualityOfService.AtMostOnce;
			var payload = "Publish Flow Test";
			var publish = new Publish (topic, qos, retain: true, duplicatedDelivery: false);

			publish.Payload = Encoding.UTF8.GetBytes (payload);

			var channel = new Mock<IChannel<IPacket>> ();

			await flow.ExecuteAsync (clientId, publish, channel.Object);

			retainedRepository.Verify (r => r.Create (It.Is<RetainedMessage> (m => m.Topic == topic && m.QualityOfService == qos && m.Payload.ToList().SequenceEqual(publish.Payload))));
			channel.Verify (c => c.SendAsync (It.IsAny<IPacket> ()), Times.Never);
		}

		[Fact]
		public async Task when_sending_publish_with_retain_then_retain_message_is_replaced()
		{
			var configuration = Mock.Of<IProtocolConfiguration> ();
			var clientManager = Mock.Of<IClientManager> ();
			var topicEvaluator = new Mock<ITopicEvaluator> ();
			var retainedRepository = new Mock<IRepository<RetainedMessage>> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();
			var packetIdentifierRepository = Mock.Of<IRepository<PacketIdentifier>> ();

			var topic = "foo/bar";

			var flow = new PublishFlow (configuration, clientManager, topicEvaluator.Object, retainedRepository.Object, 
				sessionRepository.Object, packetIdentifierRepository);
			var sessions = new List<ClientSession> { new ClientSession { ClientId = Guid.NewGuid().ToString(), Clean = false }};

			var existingRetainedMessage = new RetainedMessage();

			retainedRepository.Setup (r => r.Get (It.IsAny<Expression<Func<RetainedMessage, bool>>>())).Returns (existingRetainedMessage);
			sessionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSession, bool>>> ())).Returns (sessions.AsQueryable());

			var clientId = Guid.NewGuid ().ToString ();
			var qos = QualityOfService.AtMostOnce;
			var payload = "Publish Flow Test";
			var publish = new Publish (topic, qos, retain: true, duplicatedDelivery: false);

			publish.Payload = Encoding.UTF8.GetBytes (payload);

			var channel = new Mock<IChannel<IPacket>> ();

			await flow.ExecuteAsync (clientId, publish, channel.Object);

			retainedRepository.Verify (r => r.Delete (It.Is<RetainedMessage> (m => m == existingRetainedMessage)));
			retainedRepository.Verify (r => r.Create (It.Is<RetainedMessage> (m => m.Topic == topic && m.QualityOfService == qos && m.Payload.ToList().SequenceEqual(publish.Payload))));
			channel.Verify (c => c.SendAsync (It.IsAny<IPacket> ()), Times.Never);
		}

		[Fact]
		public async Task when_sending_publish_with_qos_higher_than_supported_then_supported_is_used()
		{
			var configuration = new Mock<IProtocolConfiguration> ();
			var clientManager = new Mock<IClientManager> ();
			var topicEvaluator = new Mock<ITopicEvaluator> ();
			var retainedRepository = new Mock<IRepository<RetainedMessage>> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();
			var packetIdentifierRepository = Mock.Of<IRepository<PacketIdentifier>> ();

			var topic = "foo/bar";

			var flow = new PublishFlow (configuration.Object, clientManager.Object, topicEvaluator.Object, retainedRepository.Object, 
				sessionRepository.Object, packetIdentifierRepository);

			var subscribedClientId = Guid.NewGuid().ToString();
			var requestedQoS = QualityOfService.ExactlyOnce;
			var sessions = new List<ClientSession> { new ClientSession {
				ClientId = subscribedClientId,
				Clean = false,
				Subscriptions = new List<ClientSubscription> { 
					new ClientSubscription { ClientId = subscribedClientId, MaximumQualityOfService = requestedQoS, TopicFilter = topic }
				}
			}};

			topicEvaluator.Setup (e => e.Matches (It.IsAny<string> (), It.IsAny<string> ())).Returns (true);
			sessionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSession, bool>>> ())).Returns (sessions.AsQueryable());

			var supportedQos = QualityOfService.AtLeastOnce;

			configuration.Setup (c => c.MaximumQualityOfService).Returns (supportedQos);

			var clientId = Guid.NewGuid ().ToString ();
			var packetId = (ushort?)new Random ().Next (0, ushort.MaxValue);
			var publish = new Publish (topic, QualityOfService.ExactlyOnce, retain: false, duplicatedDelivery: false, packetId: packetId);

			publish.Payload = Encoding.UTF8.GetBytes ("Publish Flow Test");

			var channel = new Mock<IChannel<IPacket>> ();
			var response = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (p => response = p)
				.Returns(Task.Delay(0));

			await flow.ExecuteAsync (clientId, publish, channel.Object);

			clientManager.Verify (m => m.SendMessageAsync (It.Is<string> (s => s == subscribedClientId),
				It.Is<IPacket> (p => p is Publish && ((Publish)p).Topic == topic && ((Publish)p).QualityOfService == supportedQos && ((Publish)p).PacketId.HasValue)));
			retainedRepository.Verify(r => r.Create (It.IsAny<RetainedMessage> ()), Times.Never);
			Assert.NotNull (response);

			var publishAck = response as PublishAck;

			Assert.NotNull (publishAck);
		}

		[Fact]
		public async Task when_sending_publish_ack_then_packet_identifier_is_deleted()
		{
			var configuration = Mock.Of<IProtocolConfiguration> ();
			var clientManager = Mock.Of<IClientManager> ();
			var topicEvaluator = new Mock<ITopicEvaluator> ();
			var retainedRepository = Mock.Of<IRepository<RetainedMessage>> ();
			var sessionRepository = Mock.Of<IRepository<ClientSession>> ();
			var packetIdentifierRepository = new Mock<IRepository<PacketIdentifier>> ();

			var clientId = Guid.NewGuid().ToString();
			var packetId = (ushort)new Random ().Next (0, ushort.MaxValue);
			var publishAck = new PublishAck (packetId);

			var flow = new PublishFlow (configuration, clientManager, topicEvaluator.Object, retainedRepository, sessionRepository, packetIdentifierRepository.Object);

			var channel = new Mock<IChannel<IPacket>> ();

			var response = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (p => response = p)
				.Returns(Task.Delay(0));

			await flow.ExecuteAsync (clientId, publishAck, channel.Object);

			packetIdentifierRepository.Verify (r => r.Delete (It.IsAny<Expression<Func<PacketIdentifier, bool>>> ()));
			Assert.Null (response);
		}

		[Fact]
		public async Task when_sending_publish_complete_then_packet_identifier_is_deleted()
		{
			var configuration = Mock.Of<IProtocolConfiguration> ();
			var clientManager = Mock.Of<IClientManager> ();
			var topicEvaluator = new Mock<ITopicEvaluator> ();
			var retainedRepository = Mock.Of<IRepository<RetainedMessage>> ();
			var sessionRepository = Mock.Of<IRepository<ClientSession>> ();
			var packetIdentifierRepository = new Mock<IRepository<PacketIdentifier>> ();

			var clientId = Guid.NewGuid().ToString();
			var packetId = (ushort)new Random ().Next (0, ushort.MaxValue);
			var publishComplete = new PublishComplete (packetId);

			var flow = new PublishFlow (configuration, clientManager, topicEvaluator.Object, retainedRepository, sessionRepository, packetIdentifierRepository.Object);

			var channel = new Mock<IChannel<IPacket>> ();

			var response = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (p => response = p)
				.Returns(Task.Delay(0));

			await flow.ExecuteAsync (clientId, publishComplete, channel.Object);

			packetIdentifierRepository.Verify (r => r.Delete (It.IsAny<Expression<Func<PacketIdentifier, bool>>> ()));
			Assert.Null (response);
		}

		[Fact]
		public void when_sending_publish_with_qos_higher_than_zero_and_without_packet_id_then_fails()
		{
			var configuration = new Mock<IProtocolConfiguration> ();
			var clientManager = Mock.Of<IClientManager> ();
			var topicEvaluator = new Mock<ITopicEvaluator> ();
			var retainedRepository = Mock.Of<IRepository<RetainedMessage>> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();
			var packetIdentifierRepository = Mock.Of<IRepository<PacketIdentifier>> ();

			var topic = "foo/bar";

			var flow = new PublishFlow (configuration.Object, clientManager, topicEvaluator.Object, retainedRepository,
				sessionRepository.Object, packetIdentifierRepository);

			var subscribedClientId = Guid.NewGuid().ToString();
			var sessions = new List<ClientSession> { new ClientSession { ClientId = subscribedClientId, Clean = false } };

			sessionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSession, bool>>> ())).Returns (sessions.AsQueryable());
			configuration.Setup (c => c.MaximumQualityOfService).Returns (QualityOfService.ExactlyOnce);

			var clientId = Guid.NewGuid ().ToString ();
			var publish = new Publish (topic, QualityOfService.AtLeastOnce, retain: false, duplicatedDelivery: false);

			publish.Payload = Encoding.UTF8.GetBytes ("Publish Flow Test");

			var channel = new Mock<IChannel<IPacket>> ();

			var response = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (p => response = p)
				.Returns(Task.Delay(0));

			var ex = Assert.Throws<AggregateException> (() => flow.ExecuteAsync (clientId, publish, channel.Object).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}

		[Fact]
		public void when_sending_invalid_packet_to_publish_then_fails()
		{
			var configuration = Mock.Of<IProtocolConfiguration> ();
			var clientManager = Mock.Of<IClientManager> ();
			var topicEvaluator = new Mock<ITopicEvaluator> ();
			var retainedRepository = Mock.Of<IRepository<RetainedMessage>> ();
			var sessionRepository = Mock.Of<IRepository<ClientSession>> ();
			var packetIdentifierRepository = Mock.Of<IRepository<PacketIdentifier>> ();

			var flow = new PublishFlow (configuration, clientManager, topicEvaluator.Object, retainedRepository, 
				sessionRepository, packetIdentifierRepository);

			var clientId = Guid.NewGuid ().ToString ();
			var channel = new Mock<IChannel<IPacket>> ();
			var sentPacket = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sentPacket = packet)
				.Returns(Task.Delay(0));

			var ex = Assert.Throws<AggregateException> (() => flow.ExecuteAsync (clientId, new Disconnect(), channel.Object).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}
	}
}
