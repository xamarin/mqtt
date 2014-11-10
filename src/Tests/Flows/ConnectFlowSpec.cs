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
			var sessionRepository = new Mock<IRepository<ProtocolSession>> ();
			var willRepository = new Mock<IRepository<ConnectionWill>> ();

			var sessionDeleted = false;
			var willCreated = false;

			sessionRepository.Setup (r => r.Delete (It.IsAny<Expression<Func<ProtocolSession, bool>>> ())).Callback (() => sessionDeleted = true);
			willRepository.Setup (r => r.Create (It.IsAny<ConnectionWill> ())).Callback (() => willCreated = true);

			var flow = new ConnectFlow (sessionRepository.Object, willRepository.Object);

			var clientId = Guid.NewGuid ().ToString ();
			var connect = new Connect (clientId, cleanSession: true);
			var channel = new Mock<IChannel<IPacket>> ();
			var sentPacket = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sentPacket = packet)
				.Returns(Task.Delay(0));

			await flow.ExecuteAsync (clientId, connect, channel.Object);

			sessionRepository.Verify (r => r.Create (It.Is<ProtocolSession> (s => s.ClientId == clientId && s.Clean == true)));
			Assert.NotNull (sentPacket);

			var connectAck = sentPacket as ConnectAck;

			Assert.NotNull (connectAck);
			Assert.Equal (PacketType.ConnectAck, connectAck.Type);
			Assert.Equal (ConnectionStatus.Accepted, connectAck.Status);
			Assert.False (connectAck.ExistingSession);
			Assert.False (sessionDeleted);
			Assert.False (willCreated);
		}

		[Fact]
		public async Task when_sending_connect_with_existing_session_and_without_clean_session_then_ack_is_sent()
		{
			var sessionRepository = new Mock<IRepository<ProtocolSession>> ();
			var willRepository = new Mock<IRepository<ConnectionWill>> ();

			var sessionCreated = false;
			var sessionDeleted = false;
			var willCreated = false;

			sessionRepository.Setup (r => r.Create (It.IsAny<ProtocolSession> ())).Callback (() => sessionCreated = true);
			sessionRepository.Setup (r => r.Delete (It.IsAny<Expression<Func<ProtocolSession, bool>>> ())).Callback (() => sessionDeleted = true);
			willRepository.Setup (r => r.Create (It.IsAny<ConnectionWill> ())).Callback (() => willCreated = true);

			var clientId = Guid.NewGuid ().ToString ();
			var existingSession = new ProtocolSession { ClientId = clientId, Clean = false };

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ProtocolSession, bool>>>()))
				.Returns (existingSession);

			var flow = new ConnectFlow (sessionRepository.Object, willRepository.Object);

			var connect = new Connect (clientId, cleanSession: false);
			var channel = new Mock<IChannel<IPacket>> ();
			var sentPacket = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sentPacket = packet)
				.Returns(Task.Delay(0));

			await flow.ExecuteAsync (clientId, connect, channel.Object);

			var connectAck = sentPacket as ConnectAck;

			Assert.NotNull (connectAck);
			Assert.Equal (PacketType.ConnectAck, connectAck.Type);
			Assert.Equal (ConnectionStatus.Accepted, connectAck.Status);
			Assert.True (connectAck.ExistingSession);
			Assert.False(sessionCreated);
			Assert.False(sessionDeleted);
			Assert.False(willCreated);
		}

		[Fact]
		public async Task when_sending_connect_with_existing_session_and_clean_session_then_session_is_deleted_and_ack_is_sent()
		{
			var sessionRepository = new Mock<IRepository<ProtocolSession>> ();
			var willRepository = new Mock<IRepository<ConnectionWill>> ();

			var willCreated = false;

			willRepository.Setup (r => r.Create (It.IsAny<ConnectionWill> ())).Callback (() => willCreated = true);

			var clientId = Guid.NewGuid ().ToString ();
			var existingSession = new ProtocolSession { ClientId = clientId, Clean = true };

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ProtocolSession, bool>>>()))
				.Returns (existingSession);

			var flow = new ConnectFlow (sessionRepository.Object, willRepository.Object);

			var connect = new Connect (clientId, cleanSession: true);
			var channel = new Mock<IChannel<IPacket>> ();
			var sentPacket = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sentPacket = packet)
				.Returns(Task.Delay(0));

			await flow.ExecuteAsync (clientId, connect, channel.Object);

			var connectAck = sentPacket as ConnectAck;

			sessionRepository.Verify (r => r.Delete (It.Is<ProtocolSession> (s => s == existingSession)));

			Assert.NotNull (connectAck);
			Assert.Equal (PacketType.ConnectAck, connectAck.Type);
			Assert.Equal (ConnectionStatus.Accepted, connectAck.Status);
			Assert.False (connectAck.ExistingSession);
			Assert.False(willCreated);
		}

		[Fact]
		public async Task when_sending_connect_with_will_then_will_is_created_and_ack_is_sent()
		{
			var sessionRepository = new Mock<IRepository<ProtocolSession>> ();
			var willRepository = new Mock<IRepository<ConnectionWill>> ();

			var sessionDeleted = false;

			sessionRepository.Setup (r => r.Delete (It.IsAny<Expression<Func<ProtocolSession, bool>>> ())).Callback (() => sessionDeleted = true);

			var flow = new ConnectFlow (sessionRepository.Object, willRepository.Object);

			var clientId = Guid.NewGuid ().ToString ();
			var connect = new Connect (clientId, cleanSession: true);

			var will = new Will ("foo/bar", QualityOfService.AtLeastOnce, retain: true, message: "Foo Will Message");

			connect.Will = will;

			var channel = new Mock<IChannel<IPacket>> ();
			var sentPacket = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sentPacket = packet)
				.Returns(Task.Delay(0));

			await flow.ExecuteAsync (clientId, connect, channel.Object);

			var connectAck = sentPacket as ConnectAck;

			sessionRepository.Verify (r => r.Create (It.Is<ProtocolSession> (s => s.ClientId == clientId && s.Clean == true)));
			willRepository.Verify (r => r.Create (It.Is<ConnectionWill> (w => w.ClientId == clientId && w.Will == will)));

			Assert.NotNull (connectAck);
			Assert.Equal (PacketType.ConnectAck, connectAck.Type);
			Assert.Equal (ConnectionStatus.Accepted, connectAck.Status);
			Assert.False (connectAck.ExistingSession);
			Assert.False (sessionDeleted);
		}

		[Fact]
		public void when_sending_invalid_packet_to_connect_then_fails()
		{
			var sessionRepository = new Mock<IRepository<ProtocolSession>> ();
			var willRepository = Mock.Of<IRepository<ConnectionWill>> ();

			var flow = new ConnectFlow (sessionRepository.Object, willRepository);

			var clientId = Guid.NewGuid ().ToString ();
			var invalid = new PingRequest ();
			var channel = new Mock<IChannel<IPacket>> ();
			var sentPacket = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sentPacket = packet)
				.Returns(Task.Delay(0));

			var ex = Assert.Throws<AggregateException> (() => flow.ExecuteAsync (clientId, invalid, channel.Object).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}
	}
}
