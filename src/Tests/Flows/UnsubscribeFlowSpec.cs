using System;
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
	public class UnsubscribeFlowSpec
	{
		[Fact]
		public async Task when_unsubscribing_existing_subscriptions_then_subscriptions_are_deleted_and_ack_is_sent()
		{
			var subscriptionRepository = new Mock<IRepository<ClientSubscription>> ();

			var flow = new UnsubscribeFlow (subscriptionRepository.Object);

			var clientId = Guid.NewGuid ().ToString ();
			var packetId = (ushort)new Random ().Next (0, ushort.MaxValue);
			var topic = "foo/bar/test";
			var qos = QualityOfService.AtLeastOnce;
			var existingSubscription = new ClientSubscription { ClientId = clientId, RequestedQualityOfService = qos, TopicFilter = topic };

			subscriptionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSubscription, bool>>> ())).Returns (existingSubscription);

			var unsubscribe = new Unsubscribe (packetId, topic);

			var channel = new Mock<IChannel<IPacket>> ();

			var response = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (p => response = p)
				.Returns(Task.Delay(0));

			await flow.ExecuteAsync(clientId, unsubscribe, channel.Object);

			subscriptionRepository.Verify (r => r.Delete (It.Is<ClientSubscription> (s => s == existingSubscription)));
			Assert.NotNull (response);

			var unsubscribeAck = response as UnsubscribeAck;

			Assert.NotNull (unsubscribeAck);
			Assert.Equal (packetId, unsubscribeAck.PacketId);
		}

		[Fact]
		public async Task when_unsubscribing_not_existing_subscriptions_then_ack_is_sent()
		{
			var subscriptionRepository = new Mock<IRepository<ClientSubscription>> ();

			var flow = new UnsubscribeFlow (subscriptionRepository.Object);

			var clientId = Guid.NewGuid ().ToString ();
			var packetId = (ushort)new Random ().Next (0, ushort.MaxValue);
			var deleteCalled = false;

			subscriptionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSubscription, bool>>> ())).Returns (default(ClientSubscription));
			subscriptionRepository.Setup (r => r.Delete (It.IsAny<ClientSubscription> ())).Callback(() => deleteCalled = true);

			var unsubscribe = new Unsubscribe (packetId, "foo/bar");

			var channel = new Mock<IChannel<IPacket>> ();

			var response = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (p => response = p)
				.Returns(Task.Delay(0));

			await flow.ExecuteAsync(clientId, unsubscribe, channel.Object);

			Assert.NotNull (response);

			var unsubscribeAck = response as UnsubscribeAck;

			Assert.NotNull (unsubscribeAck);
			Assert.Equal (packetId, unsubscribeAck.PacketId);
			Assert.False (deleteCalled);
		}

		[Fact]
		public void when_sending_invalid_packet_to_unsubscribe_then_fails()
		{
			var subscriptionRepository = Mock.Of<IRepository<ClientSubscription>> ();

			var flow = new UnsubscribeFlow (subscriptionRepository);

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
