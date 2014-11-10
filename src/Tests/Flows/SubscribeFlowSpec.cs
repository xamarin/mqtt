using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Hermes;
using Hermes.Flows;
using Hermes.Packets;
using Hermes.Storage;
using Moq;
using Xunit;

namespace Tests.Flows
{
	public class SubscribeFlowSpec
	{
		[Fact]
		public async Task when_subscribing_new_topics_then_subscriptions_are_created_and_ack_is_sent()
		{
			var configuration = new Mock<IProtocolConfiguration> ();
			var subscriptionRepository = new Mock<IRepository<ClientSubscription>> ();

			configuration.Setup (c => c.SupportedQualityOfService).Returns (QualityOfService.AtLeastOnce);
			subscriptionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSubscription, bool>>> ())).Returns (default (ClientSubscription));

			var flow = new SubscribeFlow (configuration.Object, subscriptionRepository.Object);

			var clientId = Guid.NewGuid().ToString();
			var fooQoS = QualityOfService.AtLeastOnce;
			var fooTopic = "test/foo/1";
			var fooSubscription = new Subscription (fooTopic, fooQoS);
			var barQoS = QualityOfService.AtMostOnce;
			var barTopic = "test/bar/1";
			var barSubscription = new Subscription (barTopic, barQoS);

			var packetId = (ushort)new Random ().Next (0, ushort.MaxValue);
			var subscribe = new Subscribe (packetId, fooSubscription, barSubscription);
			
			var channel = new Mock<IChannel<IPacket>> ();

			var response = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (p => response = p)
				.Returns(Task.Delay(0));

			await flow.ExecuteAsync (clientId, subscribe, channel.Object);

			subscriptionRepository.Verify (r => r.Create (It.Is<ClientSubscription> (s => s.ClientId == clientId && s.TopicFilter == fooTopic && s.RequestedQualityOfService == fooQoS)));
			subscriptionRepository.Verify (r => r.Create (It.Is<ClientSubscription> (s => s.ClientId == clientId && s.TopicFilter == barTopic && s.RequestedQualityOfService == barQoS)));
			Assert.NotNull (response);

			var subscribeAck = response as SubscribeAck;

			Assert.NotNull (subscribeAck);
			Assert.Equal (packetId, subscribeAck.PacketId);
			Assert.Equal (2, subscribeAck.ReturnCodes.Count ());
			Assert.True (subscribeAck.ReturnCodes.Any (c => c == SubscribeReturnCode.MaximumQoS0));
			Assert.True (subscribeAck.ReturnCodes.Any (c => c == SubscribeReturnCode.MaximumQoS1));
		}

		[Fact]
		public async Task when_subscribing_existing_topics_then_subscriptions_are_updated_and_ack_is_sent()
		{
			var configuration = new Mock<IProtocolConfiguration> ();
			var subscriptionRepository = new Mock<IRepository<ClientSubscription>> ();

			configuration.Setup (c => c.SupportedQualityOfService).Returns (QualityOfService.AtLeastOnce);

			var clientId = Guid.NewGuid().ToString();
			var fooQoS = QualityOfService.AtLeastOnce;
			var fooTopic = "test/foo/1";
			var fooSubscription = new Subscription (fooTopic, fooQoS);

			var existingSubscription = new ClientSubscription { ClientId = clientId, RequestedQualityOfService = QualityOfService.ExactlyOnce, TopicFilter = fooTopic };

			subscriptionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSubscription, bool>>> ())).Returns (existingSubscription);

			var flow = new SubscribeFlow (configuration.Object, subscriptionRepository.Object);

			var packetId = (ushort)new Random ().Next (0, ushort.MaxValue);
			var subscribe = new Subscribe (packetId, fooSubscription);
			
			var channel = new Mock<IChannel<IPacket>> ();

			var response = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (p => response = p)
				.Returns(Task.Delay(0));

			await flow.ExecuteAsync (clientId, subscribe, channel.Object);

			subscriptionRepository.Verify (r => r.Update (It.Is<ClientSubscription> (s => s.ClientId == clientId && s.TopicFilter == fooTopic && s.RequestedQualityOfService == fooQoS)));
			Assert.NotNull (response);

			var subscribeAck = response as SubscribeAck;

			Assert.NotNull (subscribeAck);
			Assert.Equal (packetId, subscribeAck.PacketId);
			Assert.Equal (1, subscribeAck.ReturnCodes.Count ());
			Assert.True (subscribeAck.ReturnCodes.Any (c => c == SubscribeReturnCode.MaximumQoS1));
		}

		[Fact]
		public void when_sending_invalid_packet_to_subscribe_then_fails()
		{
			var configuration = Mock.Of<IProtocolConfiguration> ();
			var subscriptionRepository = Mock.Of<IRepository<ClientSubscription>> ();

			var flow = new SubscribeFlow (configuration, subscriptionRepository);

			var clientId = Guid.NewGuid ().ToString ();
			var channel = new Mock<IChannel<IPacket>> ();
			var sentPacket = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sentPacket = packet)
				.Returns(Task.Delay(0));

			var ex = Assert.Throws<AggregateException> (() => flow.ExecuteAsync (clientId, new Publish("test", QualityOfService.AtMostOnce, false, false), channel.Object).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}
	}
}
