using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hermes;
using Hermes.Flows;
using Hermes.Packets;
using Hermes.Storage;
using Moq;
using Xunit;

namespace Tests.Flows
{
	public class PublishReceiverFlowSpec
	{
		[Fact]
		public async Task when_sending_publish_with_qos0_then_publish_is_sent_to_subscribers_and_no_ack_is_sent()
		{
			var clientId = Guid.NewGuid ().ToString ();

			var configuration = new ProtocolConfiguration { MaximumQualityOfService = QualityOfService.ExactlyOnce };
			var clientManager = new Mock<IClientManager> ();
			var topicEvaluator = new Mock<ITopicEvaluator> ();
			var retainedRepository = new Mock<IRepository<RetainedMessage>> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>> ()))
				.Returns (new ClientSession {
					ClientId = clientId,
					PendingMessages = new List<PendingMessage> { new PendingMessage() }
				});

			var packetIdentifierRepository = Mock.Of<IRepository<PacketIdentifier>> ();

			var topic = "foo/bar";

			var flow = new PublishReceiverFlow (configuration, clientManager.Object, topicEvaluator.Object, retainedRepository.Object, 
				sessionRepository.Object, packetIdentifierRepository);

			var subscribedClientId1 = Guid.NewGuid().ToString();
			var subscribedClientId2 = Guid.NewGuid().ToString();
			var requestedQoS1 = QualityOfService.AtLeastOnce;
			var requestedQoS2 = QualityOfService.ExactlyOnce;
			var sessions = new List<ClientSession> { 
				new ClientSession {
					ClientId = subscribedClientId1,
					Clean = false,
					Subscriptions = new List<ClientSubscription> { 
						new ClientSubscription { ClientId = subscribedClientId1, 
							MaximumQualityOfService = requestedQoS1, TopicFilter = topic }}
				},
				new ClientSession {
					ClientId = subscribedClientId2,
					Clean = false,
					Subscriptions = new List<ClientSubscription> { 
						new ClientSubscription { ClientId = subscribedClientId2, 
							MaximumQualityOfService = requestedQoS2, TopicFilter = topic }}
				}
			};

			var client1Receiver = new Subject<IPacket> ();
			var client1Channel = new Mock<IChannel<IPacket>> ();

			client1Channel.Setup (c => c.Receiver).Returns (client1Receiver);

			var client2Receiver = new Subject<IPacket> ();
			var client2Channel = new Mock<IChannel<IPacket>> ();

			client2Channel.Setup (c => c.Receiver).Returns (client2Receiver);

			clientManager.Setup (m => m.GetConnection (It.Is<string> (s => s == subscribedClientId1))).Returns (client1Channel.Object);
			clientManager.Setup (m => m.GetConnection (It.Is<string> (s => s == subscribedClientId2))).Returns (client2Channel.Object);
			topicEvaluator.Setup (e => e.Matches (It.IsAny<string> (), It.IsAny<string> ())).Returns (true);
			sessionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSession, bool>>>())).Returns (sessions.AsQueryable());

			var publish = new Publish (topic, QualityOfService.AtMostOnce, retain: false, duplicated: false);

			publish.Payload = Encoding.UTF8.GetBytes ("Publish Receiver Flow Test");

			var receiver = new Subject<IPacket> ();
			var channel = new Mock<IChannel<IPacket>> ();

			channel.Setup (c => c.Receiver).Returns (receiver);

			await flow.ExecuteAsync (clientId, publish, channel.Object);

			retainedRepository.Verify (r => r.Create (It.IsAny<RetainedMessage> ()), Times.Never);
			client1Channel.Verify (c => c.SendAsync (It.Is<IPacket> (p => p is Publish && 
				((Publish)p).Topic == topic && 
				((Publish)p).QualityOfService == requestedQoS1 && 
				((Publish)p).PacketId.HasValue)));
			client2Channel.Verify (m => m.SendAsync (It.Is<IPacket> (p => p is Publish && 
				((Publish)p).Topic == topic && 
				((Publish)p).QualityOfService == requestedQoS2 && 
				((Publish)p).PacketId.HasValue)));
			channel.Verify (c => c.SendAsync (It.IsAny<IPacket> ()), Times.Never);
		}

		[Fact]
		public async Task when_sending_publish_with_qos1_then_publish_is_sent_to_subscribers_and_publish_ack_is_sent()
		{
			var clientId = Guid.NewGuid ().ToString ();

			var configuration = new ProtocolConfiguration { MaximumQualityOfService = QualityOfService.ExactlyOnce };
			var clientManager = new Mock<IClientManager> ();
			var topicEvaluator = new Mock<ITopicEvaluator> ();
			var retainedRepository = new Mock<IRepository<RetainedMessage>> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>> ()))
				.Returns (new ClientSession {
					ClientId = clientId,
					PendingMessages = new List<PendingMessage> { new PendingMessage() }
				});

			var packetIdentifierRepository = Mock.Of<IRepository<PacketIdentifier>> ();

			var topic = "foo/bar";

			var flow = new PublishReceiverFlow (configuration, clientManager.Object, topicEvaluator.Object, 
				retainedRepository.Object, sessionRepository.Object, packetIdentifierRepository);

			var subscribedClientId = Guid.NewGuid().ToString();
			var requestedQoS = QualityOfService.ExactlyOnce;
			var sessions = new List<ClientSession> { new ClientSession {
				ClientId = subscribedClientId,
				Clean = false,
				Subscriptions = new List<ClientSubscription> { 
					new ClientSubscription { ClientId = subscribedClientId, MaximumQualityOfService = requestedQoS, TopicFilter = topic }
				}
			}};

			var clientReceiver = new Subject<IPacket> ();
			var clientChannel = new Mock<IChannel<IPacket>> ();

			clientChannel.Setup (c => c.Receiver).Returns (clientReceiver);

			clientManager.Setup (m => m.GetConnection (It.Is<string> (s => s == subscribedClientId))).Returns (clientChannel.Object);
			topicEvaluator.Setup (e => e.Matches (It.IsAny<string> (), It.IsAny<string> ())).Returns (true);
			sessionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSession, bool>>>())).Returns ( sessions.AsQueryable());

			var packetId = (ushort?)new Random ().Next (0, ushort.MaxValue);
			var publish = new Publish (topic, QualityOfService.AtLeastOnce, retain: false, duplicated: false, packetId: packetId);

			publish.Payload = Encoding.UTF8.GetBytes ("Publish Flow Test");

			var receiver = new Subject<IPacket> ();
			var channel = new Mock<IChannel<IPacket>> ();

			channel.Setup (c => c.Receiver).Returns (receiver);

			await flow.ExecuteAsync (clientId, publish, channel.Object);

			retainedRepository.Verify (r => r.Create (It.IsAny<RetainedMessage> ()), Times.Never);
			clientChannel.Verify (c => c.SendAsync (It.Is<IPacket> (p => p is Publish && 
				((Publish)p).Topic == topic && 
				((Publish)p).QualityOfService == requestedQoS && 
				((Publish)p).PacketId.HasValue)));
			channel.Verify (c => c.SendAsync (It.Is<IPacket> (p => p is PublishAck && 
				(p as PublishAck).PacketId == packetId.Value)));
		}

		[Fact]
		public async Task when_sending_publish_with_qos2_then_publish_is_sent_to_subscribers_and_publish_received_is_sent()
		{
			var clientId = Guid.NewGuid ().ToString ();

			var configuration = new ProtocolConfiguration { MaximumQualityOfService = QualityOfService.ExactlyOnce };
			var clientManager = new Mock<IClientManager> ();
			var topicEvaluator = new Mock<ITopicEvaluator> ();
			var retainedRepository = new Mock<IRepository<RetainedMessage>> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>> ()))
				.Returns (new ClientSession {
					ClientId = clientId,
					PendingMessages = new List<PendingMessage> { new PendingMessage() }
				});

			var packetIdentifierRepository = Mock.Of<IRepository<PacketIdentifier>> ();

			var topic = "foo/bar";

			var flow = new PublishReceiverFlow (configuration, clientManager.Object, topicEvaluator.Object, 
				retainedRepository.Object, sessionRepository.Object, packetIdentifierRepository);

			var subscribedClientId = Guid.NewGuid().ToString();
			var requestedQoS = QualityOfService.ExactlyOnce;
			var sessions = new List<ClientSession> { new ClientSession {
				ClientId = subscribedClientId,
				Clean = false,
				Subscriptions = new List<ClientSubscription> { 
					new ClientSubscription { ClientId = subscribedClientId, MaximumQualityOfService = requestedQoS, TopicFilter = topic }
				}
			}};

			var clientReceiver = new Subject<IPacket> ();
			var clientChannel = new Mock<IChannel<IPacket>> ();

			clientChannel.Setup (c => c.Receiver).Returns (clientReceiver);

			clientManager.Setup (m => m.GetConnection (It.Is<string> (s => s == subscribedClientId))).Returns (clientChannel.Object);
			topicEvaluator.Setup (e => e.Matches (It.IsAny<string> (), It.IsAny<string> ())).Returns (true);
			sessionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSession, bool>>>())).Returns ( sessions.AsQueryable());

			var packetId = (ushort?)new Random ().Next (0, ushort.MaxValue);
			var publish = new Publish (topic, QualityOfService.ExactlyOnce, retain: false, duplicated: false, packetId: packetId);

			publish.Payload = Encoding.UTF8.GetBytes ("Publish Flow Test");

			var receiver = new Subject<IPacket> ();
			var channel = new Mock<IChannel<IPacket>> ();

			channel.Setup (c => c.Receiver).Returns (receiver);

			await flow.ExecuteAsync (clientId, publish, channel.Object);

			clientChannel.Verify (c => c.SendAsync (It.Is<IPacket> (p => p is Publish && 
				((Publish)p).Topic == topic && 
				((Publish)p).QualityOfService == requestedQoS && 
				((Publish)p).PacketId.HasValue)));
			retainedRepository.Verify (r => r.Create (It.IsAny<RetainedMessage> ()), Times.Never);
			channel.Verify (c => c.SendAsync (It.Is<IPacket> (p => p is PublishReceived && (p as PublishReceived).PacketId == packetId.Value)));
		}

		[Fact]
		public async Task when_sending_publish_with_qos2_and_no_release_is_sent_after_receiving_publish_received_then_publish_received_is_re_transmitted()
		{
			var clientId = Guid.NewGuid ().ToString ();

			var configuration = new ProtocolConfiguration { 
				MaximumQualityOfService = QualityOfService.ExactlyOnce,
				WaitingTimeoutSecs = 1
			};
			var clientManager = new Mock<IClientManager> ();
			var topicEvaluator = new Mock<ITopicEvaluator> ();
			var retainedRepository = new Mock<IRepository<RetainedMessage>> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>> ()))
				.Returns (new ClientSession {
					ClientId = clientId,
					PendingMessages = new List<PendingMessage> { new PendingMessage() }
				});

			var packetIdentifierRepository = Mock.Of<IRepository<PacketIdentifier>> ();

			var topic = "foo/bar";

			var flow = new PublishReceiverFlow (configuration, clientManager.Object, topicEvaluator.Object, 
				retainedRepository.Object, sessionRepository.Object, packetIdentifierRepository);

			var subscribedClientId = Guid.NewGuid().ToString();
			var requestedQoS = QualityOfService.ExactlyOnce;
			var sessions = new List<ClientSession> { new ClientSession {
				ClientId = subscribedClientId,
				Clean = false,
				Subscriptions = new List<ClientSubscription> { 
					new ClientSubscription { ClientId = subscribedClientId, MaximumQualityOfService = requestedQoS, TopicFilter = topic }
				}
			}};

			var clientReceiver = new Subject<IPacket> ();
			var clientChannel = new Mock<IChannel<IPacket>> ();

			clientChannel.Setup (c => c.Receiver).Returns (clientReceiver);

			clientManager.Setup (m => m.GetConnection (It.Is<string> (s => s == subscribedClientId))).Returns (clientChannel.Object);
			topicEvaluator.Setup (e => e.Matches (It.IsAny<string> (), It.IsAny<string> ())).Returns (true);
			sessionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSession, bool>>>())).Returns ( sessions.AsQueryable());

			var packetId = (ushort?)new Random ().Next (0, ushort.MaxValue);
			var publish = new Publish (topic, QualityOfService.ExactlyOnce, retain: false, duplicated: false, packetId: packetId);

			publish.Payload = Encoding.UTF8.GetBytes ("Publish Receiver Flow Test");

			var receiver = new Subject<IPacket> ();
			var channel = new Mock<IChannel<IPacket>> ();

			channel.Setup (c => c.Receiver).Returns (receiver);

			await flow.ExecuteAsync (clientId, publish, channel.Object);

			Thread.Sleep (2000);

			channel.Verify (c => c.SendAsync (It.Is<IPacket> (p => p is PublishReceived 
				&& (p as PublishReceived).PacketId == packetId)), Times.Exactly(2));
		}

		[Fact]
		public async Task when_sending_publish_with_qos2_and_release_is_sent_after_receiving_publish_received_then_publish_received_is_not_re_transmitted()
		{
			var clientId = Guid.NewGuid ().ToString ();

			var configuration = new ProtocolConfiguration { 
				MaximumQualityOfService = QualityOfService.ExactlyOnce,
				WaitingTimeoutSecs = 2
			};
			var clientManager = new Mock<IClientManager> ();
			var topicEvaluator = new Mock<ITopicEvaluator> ();
			var retainedRepository = new Mock<IRepository<RetainedMessage>> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>> ()))
				.Returns (new ClientSession {
					ClientId = clientId,
					PendingMessages = new List<PendingMessage> { new PendingMessage() }
				});

			var packetIdentifierRepository = Mock.Of<IRepository<PacketIdentifier>> ();

			var topic = "foo/bar";

			var flow = new PublishReceiverFlow (configuration, clientManager.Object, topicEvaluator.Object, 
				retainedRepository.Object, sessionRepository.Object, packetIdentifierRepository);

			var subscribedClientId = Guid.NewGuid().ToString();
			var requestedQoS = QualityOfService.ExactlyOnce;
			var sessions = new List<ClientSession> { new ClientSession {
				ClientId = subscribedClientId,
				Clean = false,
				Subscriptions = new List<ClientSubscription> { 
					new ClientSubscription { ClientId = subscribedClientId, MaximumQualityOfService = requestedQoS, TopicFilter = topic }
				}
			}};

			var clientReceiver = new Subject<IPacket> ();
			var clientChannel = new Mock<IChannel<IPacket>> ();

			clientChannel.Setup (c => c.Receiver).Returns (clientReceiver);

			clientManager.Setup (m => m.GetConnection (It.Is<string> (s => s == subscribedClientId))).Returns (clientChannel.Object);
			topicEvaluator.Setup (e => e.Matches (It.IsAny<string> (), It.IsAny<string> ())).Returns (true);
			sessionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSession, bool>>>())).Returns ( sessions.AsQueryable());

			var packetId = (ushort?)new Random ().Next (0, ushort.MaxValue);
			var publish = new Publish (topic, QualityOfService.ExactlyOnce, retain: false, duplicated: false, packetId: packetId);

			publish.Payload = Encoding.UTF8.GetBytes ("Publish Receiver Flow Test");

			var receiver = new Subject<IPacket> ();
			var sender = new Subject<IPacket> ();
			var channel = new Mock<IChannel<IPacket>> ();

			sender.OfType<PublishReceived>().Subscribe (received => {
				receiver.OnNext (new PublishRelease (received.PacketId));
			});

			channel.Setup (c => c.Receiver).Returns (receiver);
			channel.Setup (c => c.Sender).Returns (sender);
			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sender.OnNext (packet))
				.Returns(Task.Delay(0));

			await flow.ExecuteAsync (clientId, publish, channel.Object);

			Thread.Sleep (2000);

			channel.Verify (c => c.SendAsync (It.Is<IPacket> (p => p is PublishReceived 
				&& (p as PublishReceived).PacketId == packetId)), Times.Once);
		}

		[Fact]
		public async Task when_sending_publish_with_retain_then_retain_message_is_created()
		{
			var clientId = Guid.NewGuid ().ToString ();

			var configuration = Mock.Of<ProtocolConfiguration> ();
			var clientManager = Mock.Of<IClientManager> ();
			var topicEvaluator = new Mock<ITopicEvaluator> ();
			var retainedRepository = new Mock<IRepository<RetainedMessage>> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>> ()))
				.Returns (new ClientSession {
					ClientId = clientId,
					PendingMessages = new List<PendingMessage> { new PendingMessage() }
				});

			var packetIdentifierRepository = Mock.Of<IRepository<PacketIdentifier>> ();

			var topic = "foo/bar";

			var flow = new PublishReceiverFlow (configuration, clientManager, topicEvaluator.Object, retainedRepository.Object, 
				sessionRepository.Object, packetIdentifierRepository);
			var sessions = new List<ClientSession> { new ClientSession { ClientId = Guid.NewGuid ().ToString (), Clean = false }};

			retainedRepository.Setup (r => r.Get (It.IsAny<Expression<Func<RetainedMessage, bool>>>())).Returns (default(RetainedMessage));
			sessionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSession, bool>>>())).Returns ( sessions.AsQueryable());

			var qos = QualityOfService.AtMostOnce;
			var payload = "Publish Flow Test";
			var publish = new Publish (topic, qos, retain: true, duplicated: false);

			publish.Payload = Encoding.UTF8.GetBytes (payload);

			var receiver = new Subject<IPacket> ();
			var channel = new Mock<IChannel<IPacket>> ();

			channel.Setup (c => c.Receiver).Returns (receiver);

			await flow.ExecuteAsync (clientId, publish, channel.Object);

			retainedRepository.Verify (r => r.Create (It.Is<RetainedMessage> (m => m.Topic == topic && m.QualityOfService == qos && m.Payload.ToList().SequenceEqual(publish.Payload))));
			channel.Verify (c => c.SendAsync (It.IsAny<IPacket> ()), Times.Never);
		}

		[Fact]
		public async Task when_sending_publish_with_retain_then_retain_message_is_replaced()
		{
			var clientId = Guid.NewGuid ().ToString ();

			var configuration = Mock.Of<ProtocolConfiguration> ();
			var clientManager = Mock.Of<IClientManager> ();
			var topicEvaluator = new Mock<ITopicEvaluator> ();
			var retainedRepository = new Mock<IRepository<RetainedMessage>> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>> ()))
				.Returns (new ClientSession {
					ClientId = clientId,
					PendingMessages = new List<PendingMessage> { new PendingMessage() }
				});

			var packetIdentifierRepository = Mock.Of<IRepository<PacketIdentifier>> ();

			var topic = "foo/bar";

			var flow = new PublishReceiverFlow (configuration, clientManager, topicEvaluator.Object, retainedRepository.Object, 
				sessionRepository.Object, packetIdentifierRepository);
			var sessions = new List<ClientSession> { new ClientSession { ClientId = Guid.NewGuid().ToString(), Clean = false }};

			var existingRetainedMessage = new RetainedMessage();

			retainedRepository.Setup (r => r.Get (It.IsAny<Expression<Func<RetainedMessage, bool>>>())).Returns (existingRetainedMessage);
			sessionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSession, bool>>> ())).Returns (sessions.AsQueryable());

			var qos = QualityOfService.AtMostOnce;
			var payload = "Publish Flow Test";
			var publish = new Publish (topic, qos, retain: true, duplicated: false);

			publish.Payload = Encoding.UTF8.GetBytes (payload);

			var receiver = new Subject<IPacket> ();
			var channel = new Mock<IChannel<IPacket>> ();

			channel.Setup (c => c.Receiver).Returns (receiver);

			await flow.ExecuteAsync (clientId, publish, channel.Object);

			retainedRepository.Verify (r => r.Delete (It.Is<RetainedMessage> (m => m == existingRetainedMessage)));
			retainedRepository.Verify (r => r.Create (It.Is<RetainedMessage> (m => m.Topic == topic && m.QualityOfService == qos && m.Payload.ToList().SequenceEqual(publish.Payload))));
			channel.Verify (c => c.SendAsync (It.IsAny<IPacket> ()), Times.Never);
		}

		[Fact]
		public async Task when_sending_publish_with_qos_higher_than_supported_then_supported_is_used()
		{
			var clientId = Guid.NewGuid ().ToString ();

			var configuration = new ProtocolConfiguration { MaximumQualityOfService = QualityOfService.AtLeastOnce };
			var clientManager = new Mock<IClientManager> ();
			var topicEvaluator = new Mock<ITopicEvaluator> ();
			var retainedRepository = new Mock<IRepository<RetainedMessage>> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>> ()))
				.Returns (new ClientSession {
					ClientId = clientId,
					PendingMessages = new List<PendingMessage> { new PendingMessage() }
				});

			var packetIdentifierRepository = Mock.Of<IRepository<PacketIdentifier>> ();

			var topic = "foo/bar";

			var flow = new PublishReceiverFlow (configuration, clientManager.Object, topicEvaluator.Object, retainedRepository.Object, 
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

			var clientReceiver = new Subject<IPacket> ();
			var clientChannel = new Mock<IChannel<IPacket>> ();

			clientChannel.Setup (c => c.Receiver).Returns (clientReceiver);

			clientManager.Setup (m => m.GetConnection (It.Is<string> (s => s == subscribedClientId))).Returns (clientChannel.Object);
			topicEvaluator.Setup (e => e.Matches (It.IsAny<string> (), It.IsAny<string> ())).Returns (true);
			sessionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSession, bool>>> ())).Returns (sessions.AsQueryable());

			var packetId = (ushort?)new Random ().Next (0, ushort.MaxValue);
			var publish = new Publish (topic, QualityOfService.ExactlyOnce, retain: false, duplicated: false, packetId: packetId);

			publish.Payload = Encoding.UTF8.GetBytes ("Publish Flow Test");

			var receiver = new Subject<IPacket> ();
			var channel = new Mock<IChannel<IPacket>> ();

			channel.Setup (c => c.Receiver).Returns (receiver);

			await flow.ExecuteAsync (clientId, publish, channel.Object);

			clientChannel.Verify (c => c.SendAsync (It.Is<IPacket> (p => p is Publish && 
				((Publish)p).Topic == topic && 
				((Publish)p).QualityOfService == configuration.MaximumQualityOfService && 
				((Publish)p).PacketId.HasValue)));
			retainedRepository.Verify(r => r.Create (It.IsAny<RetainedMessage> ()), Times.Never);
			channel.Verify (c => c.SendAsync (It.Is<IPacket> (p => p is PublishAck && (p as PublishAck).PacketId == packetId.Value)));
		}
		
		[Fact]
		public void when_sending_publish_with_qos_higher_than_zero_and_without_packet_id_then_fails()
		{
			var clientId = Guid.NewGuid ().ToString ();

			var configuration = new ProtocolConfiguration { MaximumQualityOfService = QualityOfService.ExactlyOnce };
			var clientManager = Mock.Of<IClientManager> ();
			var topicEvaluator = new Mock<ITopicEvaluator> ();
			var retainedRepository = Mock.Of<IRepository<RetainedMessage>> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>> ()))
				.Returns (new ClientSession {
					ClientId = clientId,
					PendingMessages = new List<PendingMessage> { new PendingMessage() }
				});

			var packetIdentifierRepository = Mock.Of<IRepository<PacketIdentifier>> ();

			var topic = "foo/bar";

			var flow = new PublishReceiverFlow (configuration, clientManager, topicEvaluator.Object, retainedRepository,
				sessionRepository.Object, packetIdentifierRepository);

			var subscribedClientId = Guid.NewGuid().ToString();
			var sessions = new List<ClientSession> { new ClientSession { ClientId = subscribedClientId, Clean = false } };

			sessionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSession, bool>>> ())).Returns (sessions.AsQueryable());

			var publish = new Publish (topic, QualityOfService.AtLeastOnce, retain: false, duplicated: false);

			publish.Payload = Encoding.UTF8.GetBytes ("Publish Flow Test");

			var receiver = new Subject<IPacket> ();
			var channel = new Mock<IChannel<IPacket>> ();

			channel.Setup (c => c.Receiver).Returns (receiver);

			var ex = Assert.Throws<AggregateException> (() => flow.ExecuteAsync (clientId, publish, channel.Object).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}

		[Fact]
		public async Task when_sending_publish_and_subscriber_with_qos1_does_not_send_publish_ack_then_publish_is_re_transmitted()
		{
			var clientId = Guid.NewGuid ().ToString ();

			var configuration = new ProtocolConfiguration { 
				MaximumQualityOfService = QualityOfService.ExactlyOnce,
				WaitingTimeoutSecs = 1
			};
			var clientManager = new Mock<IClientManager> ();
			var topicEvaluator = new Mock<ITopicEvaluator> ();
			var retainedRepository = new Mock<IRepository<RetainedMessage>> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>> ()))
				.Returns (new ClientSession {
					ClientId = clientId,
					PendingMessages = new List<PendingMessage> { new PendingMessage() }
				});

			var packetIdentifierRepository = new Mock<IRepository<PacketIdentifier>> ();

			packetIdentifierRepository.Setup (r => r.Exist (It.IsAny<Expression<Func<PacketIdentifier, bool>>> ()))
				.Returns (false);

			var topic = "foo/bar";

			var flow = new PublishReceiverFlow (configuration, clientManager.Object, topicEvaluator.Object, 
				retainedRepository.Object, sessionRepository.Object, packetIdentifierRepository.Object);

			var subscribedClientId = Guid.NewGuid().ToString();
			var requestedQoS = QualityOfService.AtLeastOnce;
			var sessions = new List<ClientSession> { new ClientSession {
				ClientId = subscribedClientId,
				Clean = false,
				Subscriptions = new List<ClientSubscription> { 
					new ClientSubscription { ClientId = subscribedClientId, MaximumQualityOfService = requestedQoS, TopicFilter = topic }
				}
			}};

			var clientReceiver = new Subject<IPacket> ();
			var clientChannel = new Mock<IChannel<IPacket>> ();

			clientChannel.Setup (c => c.Receiver).Returns (clientReceiver);

			clientManager.Setup (m => m.GetConnection (It.Is<string> (s => s == subscribedClientId))).Returns (clientChannel.Object);
			topicEvaluator.Setup (e => e.Matches (It.IsAny<string> (), It.IsAny<string> ())).Returns (true);
			sessionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSession, bool>>>())).Returns ( sessions.AsQueryable());

			var packetId = (ushort?)new Random ().Next (0, ushort.MaxValue);
			var publish = new Publish (topic, QualityOfService.ExactlyOnce, retain: false, duplicated: false, packetId: packetId);

			publish.Payload = Encoding.UTF8.GetBytes ("Publish Receiver Flow Test");

			var receiver = new Subject<IPacket> ();
			var channel = new Mock<IChannel<IPacket>> ();

			channel.Setup (c => c.Receiver).Returns (receiver);

			await flow.ExecuteAsync (clientId, publish, channel.Object);

			Thread.Sleep (2000);

			clientChannel.Verify (c => c.SendAsync (It.Is<IPacket> (p => p is Publish)), Times.Exactly(2));
		}

		[Fact]
		public async Task when_sending_publish_and_subscriber_with_qos1_send_publish_ack_then_publish_is_not_re_transmitted()
		{
			var clientId = Guid.NewGuid ().ToString ();

			var configuration = new ProtocolConfiguration { 
				MaximumQualityOfService = QualityOfService.ExactlyOnce,
				WaitingTimeoutSecs = 2
			};
			var clientManager = new Mock<IClientManager> ();
			var topicEvaluator = new Mock<ITopicEvaluator> ();
			var retainedRepository = new Mock<IRepository<RetainedMessage>> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>> ()))
				.Returns (new ClientSession {
					ClientId = clientId,
					PendingMessages = new List<PendingMessage> { new PendingMessage() }
				});

			var packetIdentifierRepository = Mock.Of<IRepository<PacketIdentifier>> ();

			var topic = "foo/bar";

			var flow = new PublishReceiverFlow (configuration, clientManager.Object, topicEvaluator.Object, 
				retainedRepository.Object, sessionRepository.Object, packetIdentifierRepository);

			var subscribedClientId = Guid.NewGuid().ToString();
			var requestedQoS = QualityOfService.AtLeastOnce;
			var sessions = new List<ClientSession> { new ClientSession {
				ClientId = subscribedClientId,
				Clean = false,
				Subscriptions = new List<ClientSubscription> { 
					new ClientSubscription { ClientId = subscribedClientId, MaximumQualityOfService = requestedQoS, TopicFilter = topic }
				}
			}};

			var clientReceiver = new Subject<IPacket> ();
			var clientSender = new Subject<IPacket> ();
			var clientChannel = new Mock<IChannel<IPacket>> ();

			clientSender.OfType<Publish>().Subscribe (p => {
				clientReceiver.OnNext (new PublishAck (p.PacketId.Value));
			});

			clientChannel.Setup (c => c.Receiver).Returns (clientReceiver);
			clientChannel.Setup (c => c.Sender).Returns (clientSender);
			clientChannel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => clientSender.OnNext (packet))
				.Returns(Task.Delay(0));
			clientManager.Setup (m => m.GetConnection (It.Is<string> (s => s == subscribedClientId))).Returns (clientChannel.Object);
			topicEvaluator.Setup (e => e.Matches (It.IsAny<string> (), It.IsAny<string> ())).Returns (true);
			sessionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSession, bool>>>())).Returns ( sessions.AsQueryable());

			var packetId = (ushort?)new Random ().Next (0, ushort.MaxValue);
			var publish = new Publish (topic, QualityOfService.ExactlyOnce, retain: false, duplicated: false, packetId: packetId);

			publish.Payload = Encoding.UTF8.GetBytes ("Publish Receiver Flow Test");

			var receiver = new Subject<IPacket> ();
			var channel = new Mock<IChannel<IPacket>> ();

			channel.Setup (c => c.Receiver).Returns (receiver);

			await flow.ExecuteAsync (clientId, publish, channel.Object);

			Thread.Sleep (2000);

			clientChannel.Verify (c => c.SendAsync (It.Is<IPacket> (p => p is Publish)), Times.Once);
		}

		[Fact]
		public async Task when_sending_publish_and_subscriber_with_qos2_does_not_send_publish_received_then_publish_is_re_transmitted()
		{
			var clientId = Guid.NewGuid ().ToString ();

			var configuration = new ProtocolConfiguration { 
				MaximumQualityOfService = QualityOfService.ExactlyOnce,
				WaitingTimeoutSecs = 1
			};
			var clientManager = new Mock<IClientManager> ();
			var topicEvaluator = new Mock<ITopicEvaluator> ();
			var retainedRepository = new Mock<IRepository<RetainedMessage>> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>> ()))
				.Returns (new ClientSession {
					ClientId = clientId,
					PendingMessages = new List<PendingMessage> { new PendingMessage() }
				});

			var packetIdentifierRepository = new Mock<IRepository<PacketIdentifier>> ();

			packetIdentifierRepository.Setup (r => r.Exist (It.IsAny<Expression<Func<PacketIdentifier, bool>>> ()))
				.Returns (false);

			var topic = "foo/bar";

			var flow = new PublishReceiverFlow (configuration, clientManager.Object, topicEvaluator.Object, 
				retainedRepository.Object, sessionRepository.Object, packetIdentifierRepository.Object);

			var subscribedClientId = Guid.NewGuid().ToString();
			var requestedQoS = QualityOfService.ExactlyOnce;
			var sessions = new List<ClientSession> { new ClientSession {
				ClientId = subscribedClientId,
				Clean = false,
				Subscriptions = new List<ClientSubscription> { 
					new ClientSubscription { ClientId = subscribedClientId, MaximumQualityOfService = requestedQoS, TopicFilter = topic }
				}
			}};

			var clientReceiver = new Subject<IPacket> ();
			var clientChannel = new Mock<IChannel<IPacket>> ();

			clientChannel.Setup (c => c.Receiver).Returns (clientReceiver);
			
			clientManager.Setup (m => m.GetConnection (It.Is<string> (s => s == subscribedClientId))).Returns (clientChannel.Object);
			topicEvaluator.Setup (e => e.Matches (It.IsAny<string> (), It.IsAny<string> ())).Returns (true);
			sessionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSession, bool>>>())).Returns ( sessions.AsQueryable());

			var packetId = (ushort?)new Random ().Next (0, ushort.MaxValue);
			var publish = new Publish (topic, QualityOfService.ExactlyOnce, retain: false, duplicated: false, packetId: packetId);

			publish.Payload = Encoding.UTF8.GetBytes ("Publish Receiver Flow Test");

			var receiver = new Subject<IPacket> ();
			var channel = new Mock<IChannel<IPacket>> ();

			channel.Setup (c => c.Receiver).Returns (receiver);

			await flow.ExecuteAsync (clientId, publish, channel.Object);

			Thread.Sleep (2000);

			clientChannel.Verify (c => c.SendAsync (It.Is<IPacket> (p => p is Publish)), Times.Exactly(2));
		}

		[Fact]
		public async Task when_sending_publish_and_subscriber_with_qos2_send_publish_received_then_publish_is_not_re_transmitted()
		{
			var clientId = Guid.NewGuid ().ToString ();

			var configuration = new ProtocolConfiguration { 
				MaximumQualityOfService = QualityOfService.ExactlyOnce,
				WaitingTimeoutSecs = 2
			};
			var clientManager = new Mock<IClientManager> ();
			var topicEvaluator = new Mock<ITopicEvaluator> ();
			var retainedRepository = new Mock<IRepository<RetainedMessage>> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>> ()))
				.Returns (new ClientSession {
					ClientId = clientId,
					PendingMessages = new List<PendingMessage> { new PendingMessage() }
				});

			var packetIdentifierRepository = Mock.Of<IRepository<PacketIdentifier>> ();

			var topic = "foo/bar";

			var flow = new PublishReceiverFlow (configuration, clientManager.Object, topicEvaluator.Object, 
				retainedRepository.Object, sessionRepository.Object, packetIdentifierRepository);

			var subscribedClientId = Guid.NewGuid().ToString();
			var requestedQoS = QualityOfService.ExactlyOnce;
			var sessions = new List<ClientSession> { new ClientSession {
				ClientId = subscribedClientId,
				Clean = false,
				Subscriptions = new List<ClientSubscription> { 
					new ClientSubscription { ClientId = subscribedClientId, MaximumQualityOfService = requestedQoS, TopicFilter = topic }
				}
			}};

			var clientReceiver = new Subject<IPacket> ();
			var clientSender = new Subject<IPacket> ();
			var clientChannel = new Mock<IChannel<IPacket>> ();

			clientSender.OfType<Publish>().Subscribe (p => {
				clientReceiver.OnNext (new PublishReceived (p.PacketId.Value));
			});

			clientChannel.Setup (c => c.Receiver).Returns (clientReceiver);
			clientChannel.Setup (c => c.Sender).Returns (clientSender);
			clientChannel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => clientSender.OnNext (packet))
				.Returns(Task.Delay(0));
			clientManager.Setup (m => m.GetConnection (It.Is<string> (s => s == subscribedClientId))).Returns (clientChannel.Object);
			topicEvaluator.Setup (e => e.Matches (It.IsAny<string> (), It.IsAny<string> ())).Returns (true);
			sessionRepository.Setup (r => r.GetAll (It.IsAny<Expression<Func<ClientSession, bool>>>())).Returns ( sessions.AsQueryable());

			var packetId = (ushort?)new Random ().Next (0, ushort.MaxValue);
			var publish = new Publish (topic, QualityOfService.ExactlyOnce, retain: false, duplicated: false, packetId: packetId);

			publish.Payload = Encoding.UTF8.GetBytes ("Publish Receiver Flow Test");

			var receiver = new Subject<IPacket> ();
			var channel = new Mock<IChannel<IPacket>> ();

			channel.Setup (c => c.Receiver).Returns (receiver);

			await flow.ExecuteAsync (clientId, publish, channel.Object);

			Thread.Sleep (2000);

			clientChannel.Verify (c => c.SendAsync (It.Is<IPacket> (p => p is Publish)), Times.Once);
		}

		[Fact]
		public async Task when_sending_publish_release_then_publish_complete_is_sent()
		{
			var clientId = Guid.NewGuid ().ToString ();

			var configuration = Mock.Of<ProtocolConfiguration> ();
			var clientManager = Mock.Of<IClientManager> ();
			var topicEvaluator = new Mock<ITopicEvaluator> ();
			var retainedRepository = Mock.Of<IRepository<RetainedMessage>> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>> ()))
				.Returns (new ClientSession {
					ClientId = clientId,
					PendingMessages = new List<PendingMessage> { new PendingMessage() }
				});

			var packetIdentifierRepository = Mock.Of<IRepository<PacketIdentifier>> ();

			var flow = new PublishReceiverFlow (configuration, clientManager, topicEvaluator.Object, retainedRepository, 
				sessionRepository.Object, packetIdentifierRepository);

			var packetId = (ushort)new Random ().Next (0, ushort.MaxValue);
			var publishRelease = new PublishRelease (packetId);

			var channel = new Mock<IChannel<IPacket>> ();

			await flow.ExecuteAsync (clientId, publishRelease, channel.Object);

			channel.Verify (c => c.SendAsync (It.Is<IPacket> (p => p is PublishComplete && (p as PublishComplete).PacketId == packetId)));
		}
	}
}
