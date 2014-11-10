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
			var retainedRepository = new Mock<IRepository<RetainedMessage>> ();
			var subscriptionRepository = new Mock<IRepository<ClientSubscription>> ();

			var topic = "foo/bar";

			var flow = new PublishFlow (configuration.Object, clientManager.Object, retainedRepository.Object, subscriptionRepository.Object);

			var subscribedClientId = Guid.NewGuid().ToString();
			var requestedQoS = QualityOfService.AtLeastOnce;
			var subscriptions = new List<ClientSubscription> ();

			subscriptions.Add (new ClientSubscription { ClientId = subscribedClientId, RequestedQualityOfService = requestedQoS, TopicFilter = topic });

			var retainedMessageCreated = false;

			retainedRepository.Setup (r => r.Create (It.IsAny<RetainedMessage> ())).Callback (() => retainedMessageCreated = true);
			subscriptionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSubscription, bool>>> ()))
				.Returns (subscriptions.AsQueryable());
			configuration.Setup (c => c.SupportedQualityOfService).Returns (QualityOfService.AtLeastOnce);

			var clientId = Guid.NewGuid ().ToString ();
			var publish = new Publish (topic, QualityOfService.AtMostOnce, retain: false, duplicatedDelivery: false);

			publish.Payload = Encoding.UTF8.GetBytes ("Publish Flow Test");

			var channel = new Mock<IChannel<IPacket>> ();

			var packetResponseSent = false;

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ())).Callback (() => packetResponseSent = true);

			await flow.ExecuteAsync (clientId, publish, channel.Object);

			clientManager.Verify (m => m.SendMessageAsync (It.Is<string> (s => s == subscribedClientId),
				It.Is<IPacket> (p => p is Publish && ((Publish)p).Topic == topic && ((Publish)p).QualityOfService == requestedQoS && ((Publish)p).PacketId.HasValue)));
			Assert.False (retainedMessageCreated);
			Assert.False (packetResponseSent);
		}

		[Fact]
		public async Task when_sending_publish_with_qos1_then_publish_is_sent_to_subscribers_and_publish_ack_is_sent()
		{
			var configuration = new Mock<IProtocolConfiguration> ();
			var clientManager = new Mock<IClientManager> ();
			var retainedRepository = new Mock<IRepository<RetainedMessage>> ();
			var subscriptionRepository = new Mock<IRepository<ClientSubscription>> ();

			var topic = "foo/bar";

			var flow = new PublishFlow (configuration.Object, clientManager.Object, retainedRepository.Object, subscriptionRepository.Object);

			var subscribedClientId = Guid.NewGuid().ToString();
			var requestedQoS = QualityOfService.ExactlyOnce;
			var subscriptions = new List<ClientSubscription> ();

			subscriptions.Add (new ClientSubscription { ClientId = subscribedClientId, RequestedQualityOfService = requestedQoS, TopicFilter = topic });

			var retainedMessageCreated = false;

			retainedRepository.Setup (r => r.Create (It.IsAny<RetainedMessage> ())).Callback (() => retainedMessageCreated = true);
			subscriptionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSubscription, bool>>> ()))
				.Returns (subscriptions.AsQueryable());
			configuration.Setup (c => c.SupportedQualityOfService).Returns (QualityOfService.ExactlyOnce);

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

			clientManager.Verify (m => m.SendMessageAsync (It.Is<string> (s => s == subscribedClientId),
				It.Is<IPacket> (p => p is Publish && ((Publish)p).Topic == topic && ((Publish)p).QualityOfService == requestedQoS && ((Publish)p).PacketId.HasValue)));
			Assert.False (retainedMessageCreated);
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
			var retainedRepository = new Mock<IRepository<RetainedMessage>> ();
			var subscriptionRepository = new Mock<IRepository<ClientSubscription>> ();

			var topic = "foo/bar";

			var flow = new PublishFlow (configuration.Object, clientManager.Object, retainedRepository.Object, subscriptionRepository.Object);

			var subscribedClientId = Guid.NewGuid().ToString();
			var requestedQoS = QualityOfService.ExactlyOnce;
			var subscriptions = new List<ClientSubscription> ();

			subscriptions.Add (new ClientSubscription { ClientId = subscribedClientId, RequestedQualityOfService = requestedQoS, TopicFilter = topic });

			var retainedMessageCreated = false;

			retainedRepository.Setup (r => r.Create (It.IsAny<RetainedMessage> ())).Callback (() => retainedMessageCreated = true);
			subscriptionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSubscription, bool>>> ()))
				.Returns (subscriptions.AsQueryable());
			configuration.Setup (c => c.SupportedQualityOfService).Returns (QualityOfService.ExactlyOnce);

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
			Assert.False (retainedMessageCreated);
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
			var retainedRepository = Mock.Of<IRepository<RetainedMessage>> ();
			var subscriptionRepository = Mock.Of<IRepository<ClientSubscription>> ();

			var flow = new PublishFlow (configuration, clientManager, retainedRepository, subscriptionRepository);

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
			var retainedRepository = Mock.Of<IRepository<RetainedMessage>> ();
			var subscriptionRepository = Mock.Of<IRepository<ClientSubscription>> ();

			var flow = new PublishFlow (configuration, clientManager, retainedRepository, subscriptionRepository);

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
			var configuration = new Mock<IProtocolConfiguration> ();
			var clientManager = new Mock<IClientManager> ();
			var retainedRepository = new Mock<IRepository<RetainedMessage>> ();
			var subscriptionRepository = new Mock<IRepository<ClientSubscription>> ();

			var topic = "foo/bar";

			var flow = new PublishFlow (configuration.Object, clientManager.Object, retainedRepository.Object, subscriptionRepository.Object);

			var subscriptions = new List<ClientSubscription> ();

			retainedRepository.Setup (r => r.Get (It.IsAny<Expression<Func<RetainedMessage, bool>>>())).Returns (default(RetainedMessage));
			subscriptionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSubscription, bool>>> ()))
				.Returns (subscriptions.AsQueryable());

			var clientId = Guid.NewGuid ().ToString ();
			var qos = QualityOfService.AtMostOnce;
			var payload = "Publish Flow Test";
			var publish = new Publish (topic, qos, retain: true, duplicatedDelivery: false);

			publish.Payload = Encoding.UTF8.GetBytes (payload);

			var channel = new Mock<IChannel<IPacket>> ();

			var packetResponseSent = false;

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ())).Callback (() => packetResponseSent = true);

			await flow.ExecuteAsync (clientId, publish, channel.Object);

			retainedRepository.Verify (r => r.Create (It.Is<RetainedMessage> (m => m.Topic == topic && m.QualityOfService == qos && m.Payload == payload)));
			Assert.False (packetResponseSent);
		}

		[Fact]
		public async Task when_sending_publish_with_retain_then_retain_message_is_replaced()
		{
			var configuration = new Mock<IProtocolConfiguration> ();
			var clientManager = new Mock<IClientManager> ();
			var retainedRepository = new Mock<IRepository<RetainedMessage>> ();
			var subscriptionRepository = new Mock<IRepository<ClientSubscription>> ();

			var topic = "foo/bar";

			var flow = new PublishFlow (configuration.Object, clientManager.Object, retainedRepository.Object, subscriptionRepository.Object);

			var subscriptions = new List<ClientSubscription> ();

			var existingRetainedMessage = new RetainedMessage();

			retainedRepository.Setup (r => r.Get (It.IsAny<Expression<Func<RetainedMessage, bool>>>())).Returns (existingRetainedMessage);
			subscriptionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSubscription, bool>>> ()))
				.Returns (subscriptions.AsQueryable());

			var clientId = Guid.NewGuid ().ToString ();
			var qos = QualityOfService.AtMostOnce;
			var payload = "Publish Flow Test";
			var publish = new Publish (topic, qos, retain: true, duplicatedDelivery: false);

			publish.Payload = Encoding.UTF8.GetBytes (payload);

			var channel = new Mock<IChannel<IPacket>> ();

			var packetResponseSent = false;

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ())).Callback (() => packetResponseSent = true);

			await flow.ExecuteAsync (clientId, publish, channel.Object);

			retainedRepository.Verify (r => r.Delete (It.Is<RetainedMessage> (m => m == existingRetainedMessage)));
			retainedRepository.Verify (r => r.Create (It.Is<RetainedMessage> (m => m.Topic == topic && m.QualityOfService == qos && m.Payload == payload)));

			Assert.False (packetResponseSent);
		}

		[Fact]
		public async Task when_sending_publish_with_qos_higher_than_supported_then_supported_is_used()
		{
			var configuration = new Mock<IProtocolConfiguration> ();
			var clientManager = new Mock<IClientManager> ();
			var retainedRepository = new Mock<IRepository<RetainedMessage>> ();
			var subscriptionRepository = new Mock<IRepository<ClientSubscription>> ();

			var topic = "foo/bar";

			var flow = new PublishFlow (configuration.Object, clientManager.Object, retainedRepository.Object, subscriptionRepository.Object);

			var subscribedClientId = Guid.NewGuid().ToString();
			var requestedQoS = QualityOfService.ExactlyOnce;
			var subscriptions = new List<ClientSubscription> ();

			subscriptions.Add (new ClientSubscription { ClientId = subscribedClientId, RequestedQualityOfService = requestedQoS, TopicFilter = topic });

			var retainedMessageCreated = false;

			retainedRepository.Setup (r => r.Create (It.IsAny<RetainedMessage> ())).Callback (() => retainedMessageCreated = true);
			subscriptionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSubscription, bool>>> ()))
				.Returns (subscriptions.AsQueryable());

			var supportedQos = QualityOfService.AtLeastOnce;

			configuration.Setup (c => c.SupportedQualityOfService).Returns (supportedQos);

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
			Assert.False (retainedMessageCreated);
			Assert.NotNull (response);

			var publishAck = response as PublishAck;

			Assert.NotNull (publishAck);
		}

		[Fact]
		public void when_sending_publish_with_qos_higher_than_zero_and_without_packet_id_then_fails()
		{
			var configuration = new Mock<IProtocolConfiguration> ();
			var clientManager = new Mock<IClientManager> ();
			var retainedRepository = new Mock<IRepository<RetainedMessage>> ();
			var subscriptionRepository = new Mock<IRepository<ClientSubscription>> ();

			var topic = "foo/bar";

			var flow = new PublishFlow (configuration.Object, clientManager.Object, retainedRepository.Object, subscriptionRepository.Object);

			var subscribedClientId = Guid.NewGuid().ToString();
			var subscriptions = new List<ClientSubscription> ();

			subscriptionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSubscription, bool>>> ()))
				.Returns (subscriptions.AsQueryable());
			configuration.Setup (c => c.SupportedQualityOfService).Returns (QualityOfService.ExactlyOnce);

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
			var retainedRepository = Mock.Of<IRepository<RetainedMessage>> ();
			var subscriptionRepository = Mock.Of<IRepository<ClientSubscription>> ();

			var flow = new PublishFlow (configuration, clientManager, retainedRepository, subscriptionRepository);

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
