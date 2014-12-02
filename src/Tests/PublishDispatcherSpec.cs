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

namespace Tests
{
	public class PublishDispatcherSpec
	{
		[Fact]
		public async Task when_dispatching_publish_then_it_is_forwarded_to_subscribed_clients()
		{
			var connectionProvider = new Mock<IConnectionProvider>();
			var topicEvaluator = new Mock<ITopicEvaluator>();
			var sessionRepository = new Mock<IRepository<ClientSession>>();
			var packetIdentifierRepository = new Mock<IRepository<PacketIdentifier>>();
			var senderFlow = new Mock<IPublishSenderFlow>();
			var configuration = new ProtocolConfiguration { MaximumQualityOfService = QualityOfService.AtLeastOnce };
			var dispatcher = new PublishDispatcher (connectionProvider.Object, topicEvaluator.Object,
				sessionRepository.Object, packetIdentifierRepository.Object, senderFlow.Object, configuration);

			var client1Id = Guid.NewGuid ().ToString ();
			var subscription1 = new ClientSubscription {
				ClientId = client1Id,
				MaximumQualityOfService = QualityOfService.AtLeastOnce,
				TopicFilter = "foo/bar/#"
			};
			var subscription2 = new ClientSubscription {
				ClientId = client1Id,
				MaximumQualityOfService = QualityOfService.ExactlyOnce,
				TopicFilter = "foo/+/+"
			};
			var session1 = new ClientSession {
				ClientId = client1Id,
				Clean = false,
				Subscriptions = { subscription1, subscription2 }
			};

			var client2Id = Guid.NewGuid ().ToString ();
			var subscription3 = new ClientSubscription {
				ClientId = client2Id,
				MaximumQualityOfService = QualityOfService.AtMostOnce,
				TopicFilter = "foo/bar/test"
			};
			var session2 = new ClientSession {
				ClientId = client2Id,
				Clean = false,
				Subscriptions = { subscription3 }
			};

			var sessions = new List<ClientSession> { session1, session2 };

			sessionRepository
				.Setup(r => r.GetAll(It.IsAny<Expression<Func<ClientSession, bool>>>()))
				.Returns(sessions.AsQueryable());
			topicEvaluator
				.Setup (e => e.Matches (It.IsAny<string> (), It.IsAny<string> ()))
				.Returns(true);
			connectionProvider
				.Setup(p => p.GetConnection(It.IsAny<string>()))
				.Returns(Mock.Of<IChannel<IPacket>>());

			var topic = "foo/bar/test";
			var payload = Encoding.UTF8.GetBytes("Publish Disaptcher Test");
			var publish = new Publish(topic, QualityOfService.AtMostOnce, retain: false, duplicated: false, packetId: null)
			{
				Payload = payload
			};

			await dispatcher.DispatchAsync(publish);

			senderFlow.Verify(f => f.SendPublishAsync(It.IsAny<string>(), 
				It.Is<Publish>(p => p.Topic == topic && p.Payload == payload),
				It.IsAny<IChannel<IPacket>>(),
				It.Is<PendingMessageStatus>(s => s == PendingMessageStatus.PendingToSend)), Times.Exactly(3));		
			senderFlow.Verify(f => f.SendPublishAsync(It.Is<string>(s => s == client1Id), 
				It.Is<Publish>(p => p.Topic == topic && p.Payload == payload),
				It.IsAny<IChannel<IPacket>>(),
				It.Is<PendingMessageStatus>(s => s == PendingMessageStatus.PendingToSend)), Times.Exactly(2));		
			senderFlow.Verify(f => f.SendPublishAsync(It.Is<string>(s => s == client2Id), 
				It.Is<Publish>(p => p.Topic == topic && p.Payload == payload),
				It.IsAny<IChannel<IPacket>>(),
				It.Is<PendingMessageStatus>(s => s == PendingMessageStatus.PendingToSend)), Times.Once);		
		}
	}
}
