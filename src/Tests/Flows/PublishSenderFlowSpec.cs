using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
	public class PublishSenderFlowSpec
	{
		[Fact]
		public async Task when_sending_publish_received_then_publish_release_is_sent()
		{
			var clientId = Guid.NewGuid ().ToString ();

			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 10);
			var clientManager = Mock.Of<IClientManager> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>> ()))
				.Returns (new ClientSession {
					ClientId = clientId,
					PendingMessages = new List<PendingMessage> { new PendingMessage() }
				});

			var packetIdentifierRepository = Mock.Of<IRepository<PacketIdentifier>> ();

			var flow = new PublishSenderFlow (configuration, clientManager, 
				sessionRepository.Object, packetIdentifierRepository);

			var packetId = (ushort)new Random ().Next (0, ushort.MaxValue);
			var publishReceived = new PublishReceived (packetId);
			var receiver = new Subject<IPacket> ();
			var channel = new Mock<IChannel<IPacket>> ();

			channel.Setup (c => c.Receiver).Returns (receiver);

			await flow.ExecuteAsync (clientId, publishReceived, channel.Object);

			channel.Verify (c => c.SendAsync (It.Is<IPacket> (p => p is PublishRelease 
				&& (p as PublishRelease).PacketId == packetId)), Times.Once);
		}

		[Fact]
		public async Task when_sending_publish_received_and_no_complete_is_sent_after_receiving_publish_release_then_publish_release_is_re_transmitted()
		{
			var clientId = Guid.NewGuid ().ToString ();

			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 1);
			var clientManager = Mock.Of<IClientManager> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>> ()))
				.Returns (new ClientSession {
					ClientId = clientId,
					PendingMessages = new List<PendingMessage> { new PendingMessage() }
				});

			var packetIdentifierRepository = Mock.Of<IRepository<PacketIdentifier>> ();

			var flow = new PublishSenderFlow (configuration, clientManager, 
				sessionRepository.Object, packetIdentifierRepository);

			var packetId = (ushort)new Random ().Next (0, ushort.MaxValue);
			var publishReceived = new PublishReceived (packetId);
			var receiver = new Subject<IPacket> ();
			var channel = new Mock<IChannel<IPacket>> ();

			channel.Setup (c => c.Receiver).Returns (receiver);

			await flow.ExecuteAsync (clientId, publishReceived, channel.Object);

			Thread.Sleep (2000);

			channel.Verify (c => c.SendAsync (It.Is<IPacket> (p => p is PublishRelease 
				&& (p as PublishRelease).PacketId == packetId)), Times.Exactly(2));
		}

		[Fact]
		public async Task when_sending_publish_received_and_complete_is_sent_after_receiving_publish_release_then_publish_release_is_not_re_transmitted()
		{
			var clientId = Guid.NewGuid ().ToString ();

			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 2);
			var clientManager = Mock.Of<IClientManager> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>> ()))
				.Returns (new ClientSession {
					ClientId = clientId,
					PendingMessages = new List<PendingMessage> { new PendingMessage() }
				});

			var packetIdentifierRepository = Mock.Of<IRepository<PacketIdentifier>> ();

			var flow = new PublishSenderFlow (configuration, clientManager, 
				sessionRepository.Object, packetIdentifierRepository);

			var packetId = (ushort)new Random ().Next (0, ushort.MaxValue);
			var publishReceived = new PublishReceived (packetId);

			var receiver = new Subject<IPacket> ();
			var sender = new Subject<IPacket> ();
			var channel = new Mock<IChannel<IPacket>> ();

			sender.OfType<PublishRelease>().Subscribe (release => {
				receiver.OnNext (new PublishComplete (release.PacketId));
			});

			channel.Setup (c => c.Receiver).Returns (receiver);
			channel.Setup (c => c.Sender).Returns (sender);
			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sender.OnNext (packet))
				.Returns(Task.Delay(0));

			await flow.ExecuteAsync (clientId, publishReceived, channel.Object);

			Thread.Sleep (2000);

			channel.Verify (c => c.SendAsync (It.Is<IPacket> (p => p is PublishRelease 
				&& (p as PublishRelease).PacketId == packetId)), Times.Once);
		}

		[Fact]
		public async Task when_sending_publish_ack_then_packet_identifier_is_deleted()
		{
			var clientId = Guid.NewGuid().ToString();

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

			var packetIdentifierRepository = new Mock<IRepository<PacketIdentifier>> ();
			
			var packetId = (ushort)new Random ().Next (0, ushort.MaxValue);
			var publishAck = new PublishAck (packetId);
			var flow = new PublishSenderFlow (configuration, clientManager, 
				sessionRepository.Object, packetIdentifierRepository.Object);
			var receiver = new Subject<IPacket> ();
			var channel = new Mock<IChannel<IPacket>> ();

			channel.Setup (c => c.Receiver).Returns (receiver);

			await flow.ExecuteAsync (clientId, publishAck, channel.Object);

			packetIdentifierRepository.Verify (r => r.Delete (It.IsAny<Expression<Func<PacketIdentifier, bool>>> ()));
			channel.Verify (c => c.SendAsync (It.IsAny<IPacket>()), Times.Never);
		}

		[Fact]
		public async Task when_sending_publish_complete_then_packet_identifier_is_deleted()
		{
			var clientId = Guid.NewGuid().ToString();

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

			var packetIdentifierRepository = new Mock<IRepository<PacketIdentifier>> ();

			var packetId = (ushort)new Random ().Next (0, ushort.MaxValue);
			var publishComplete = new PublishComplete (packetId);
			var flow = new PublishSenderFlow (configuration, clientManager, 
				sessionRepository.Object, packetIdentifierRepository.Object);
			var receiver = new Subject<IPacket> ();
			var channel = new Mock<IChannel<IPacket>> ();

			channel.Setup (c => c.Receiver).Returns (receiver);

			await flow.ExecuteAsync (clientId, publishComplete, channel.Object);

			packetIdentifierRepository.Verify (r => r.Delete (It.IsAny<Expression<Func<PacketIdentifier, bool>>> ()));
			channel.Verify (c => c.SendAsync (It.IsAny<IPacket>()), Times.Never);
		}
	}
}
