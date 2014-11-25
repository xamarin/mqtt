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
	public class ConnectFlowSpec
	{
		[Fact]
		public async Task when_sending_connect_then_session_is_created_and_ack_is_sent()
		{
			var sessionRepository = new Mock<IRepository<ClientSession>> ();
			var willRepository = new Mock<IRepository<ConnectionWill>> ();

			var flow = new ConnectFlow (sessionRepository.Object, willRepository.Object);

			var clientId = Guid.NewGuid ().ToString ();
			var connect = new Connect (clientId, cleanSession: true);
			var context = new Mock<ICommunicationContext> ();
			var sentPacket = default(IPacket);

			context.Setup (c => c.PushDeliveryAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sentPacket = packet)
				.Returns(Task.Delay(0));

			await flow.ExecuteAsync (clientId, connect, context.Object);

			sessionRepository.Verify (r => r.Create (It.Is<ClientSession> (s => s.ClientId == clientId && s.Clean == true)));
			sessionRepository.Verify (r => r.Delete (It.IsAny<Expression<Func<ClientSession, bool>>> ()), Times.Never);
			willRepository.Verify (r => r.Create (It.IsAny<ConnectionWill> ()), Times.Never);

			Assert.NotNull (sentPacket);

			var connectAck = sentPacket as ConnectAck;

			Assert.NotNull (connectAck);
			Assert.Equal (PacketType.ConnectAck, connectAck.Type);
			Assert.Equal (ConnectionStatus.Accepted, connectAck.Status);
			Assert.False (connectAck.SessionPresent);
		}

		[Fact]
		public async Task when_sending_connect_with_existing_session_and_without_clean_session_then_session_is_not_deleted_and_ack_is_sent_with_session_present()
		{
			var sessionRepository = new Mock<IRepository<ClientSession>> ();
			var willRepository = new Mock<IRepository<ConnectionWill>> ();

			var clientId = Guid.NewGuid ().ToString ();
			var existingSession = new ClientSession { ClientId = clientId, Clean = false };

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>>()))
				.Returns (existingSession);

			var flow = new ConnectFlow (sessionRepository.Object, willRepository.Object);

			var connect = new Connect (clientId, cleanSession: false);
			var context = new Mock<ICommunicationContext> ();
			var sentPacket = default(IPacket);

			context.Setup (c => c.PushDeliveryAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sentPacket = packet)
				.Returns(Task.Delay(0));

			await flow.ExecuteAsync (clientId, connect, context.Object);

			sessionRepository.Verify (r => r.Create (It.IsAny<ClientSession> ()), Times.Never);
			sessionRepository.Verify (r => r.Delete (It.IsAny<Expression<Func<ClientSession, bool>>> ()), Times.Never);
			willRepository.Verify (r => r.Create (It.IsAny<ConnectionWill> ()), Times.Never);

			var connectAck = sentPacket as ConnectAck;

			Assert.NotNull (connectAck);
			Assert.Equal (PacketType.ConnectAck, connectAck.Type);
			Assert.Equal (ConnectionStatus.Accepted, connectAck.Status);
			Assert.True (connectAck.SessionPresent);
		}

		[Fact]
		public async Task when_sending_connect_with_existing_session_and_clean_session_then_session_is_deleted_and_ack_is_sent_with_session_present()
		{
			var sessionRepository = new Mock<IRepository<ClientSession>> ();
			var willRepository = new Mock<IRepository<ConnectionWill>> ();

			var clientId = Guid.NewGuid ().ToString ();
			var existingSession = new ClientSession { ClientId = clientId, Clean = true };

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>>()))
				.Returns (existingSession);

			var flow = new ConnectFlow (sessionRepository.Object, willRepository.Object);

			var connect = new Connect (clientId, cleanSession: true);
			var context = new Mock<ICommunicationContext> ();
			var sentPacket = default(IPacket);

			context.Setup (c => c.PushDeliveryAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sentPacket = packet)
				.Returns(Task.Delay(0));

			await flow.ExecuteAsync (clientId, connect, context.Object);

			var connectAck = sentPacket as ConnectAck;

			sessionRepository.Verify (r => r.Delete (It.Is<ClientSession> (s => s == existingSession)));
			sessionRepository.Verify (r => r.Create(It.Is<ClientSession> (s => s.Clean == true)));
			willRepository.Verify (r => r.Create (It.IsAny<ConnectionWill> ()), Times.Never);

			Assert.NotNull (connectAck);
			Assert.Equal (PacketType.ConnectAck, connectAck.Type);
			Assert.Equal (ConnectionStatus.Accepted, connectAck.Status);
			Assert.False (connectAck.SessionPresent);
		}

		[Fact]
		public async Task when_sending_connect_without_existing_session_and_without_clean_session_then_ack_is_sent_with_no_session_present()
		{
			var sessionRepository = new Mock<IRepository<ClientSession>> ();
			var willRepository = new Mock<IRepository<ConnectionWill>> ();

			var clientId = Guid.NewGuid ().ToString ();

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>>()))
				.Returns (default(ClientSession));

			var flow = new ConnectFlow (sessionRepository.Object, willRepository.Object);

			var connect = new Connect (clientId, cleanSession: false);
			var context = new Mock<ICommunicationContext> ();
			var sentPacket = default(IPacket);

			context.Setup (c => c.PushDeliveryAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sentPacket = packet)
				.Returns(Task.Delay(0));

			await flow.ExecuteAsync (clientId, connect, context.Object);

			var connectAck = sentPacket as ConnectAck;

			Assert.False (connectAck.SessionPresent);
		}

		[Fact]
		public async Task when_sending_connect_with_will_then_will_is_created_and_ack_is_sent()
		{
			var sessionRepository = new Mock<IRepository<ClientSession>> ();
			var willRepository = new Mock<IRepository<ConnectionWill>> ();

			var flow = new ConnectFlow (sessionRepository.Object, willRepository.Object);

			var clientId = Guid.NewGuid ().ToString ();
			var connect = new Connect (clientId, cleanSession: true);

			var will = new Will ("foo/bar", QualityOfService.AtLeastOnce, retain: true, message: "Foo Will Message");

			connect.Will = will;

			var context = new Mock<ICommunicationContext> ();
			var sentPacket = default(IPacket);

			context.Setup (c => c.PushDeliveryAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sentPacket = packet)
				.Returns(Task.Delay(0));

			await flow.ExecuteAsync (clientId, connect, context.Object);

			var connectAck = sentPacket as ConnectAck;

			sessionRepository.Verify (r => r.Delete (It.IsAny<Expression<Func<ClientSession, bool>>> ()), Times.Never);
			sessionRepository.Verify (r => r.Create (It.Is<ClientSession> (s => s.ClientId == clientId && s.Clean == true)));
			willRepository.Verify (r => r.Create (It.Is<ConnectionWill> (w => w.ClientId == clientId && w.Will == will)));

			Assert.NotNull (connectAck);
			Assert.Equal (PacketType.ConnectAck, connectAck.Type);
			Assert.Equal (ConnectionStatus.Accepted, connectAck.Status);
			Assert.False (connectAck.SessionPresent);
		}

		[Fact]
		public void when_sending_invalid_packet_to_connect_then_fails()
		{
			var sessionRepository = new Mock<IRepository<ClientSession>> ();
			var willRepository = Mock.Of<IRepository<ConnectionWill>> ();

			var flow = new ConnectFlow (sessionRepository.Object, willRepository);

			var clientId = Guid.NewGuid ().ToString ();
			var invalid = new PingRequest ();
			var context = new Mock<ICommunicationContext> ();
			var sentPacket = default(IPacket);

			context.Setup (c => c.PushDeliveryAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sentPacket = packet)
				.Returns(Task.Delay(0));

			var ex = Assert.Throws<AggregateException> (() => flow.ExecuteAsync (clientId, invalid, context.Object).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}
	}
}
