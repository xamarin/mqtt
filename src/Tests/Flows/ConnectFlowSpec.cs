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
			var authenticationProvider = Mock.Of<IAuthenticationProvider> (p => p.Authenticate (It.IsAny<string> (), It.IsAny<string> ()) == true);
			var sessionRepository = new Mock<IRepository<ClientSession>> ();
			var willRepository = new Mock<IRepository<ConnectionWill>> ();
			var senderFlow = new Mock<IPublishSenderFlow> ();

			var clientId = Guid.NewGuid ().ToString ();
			var connect = new Connect (clientId, cleanSession: true);
			var channel = new Mock<IChannel<IPacket>> ();
			var sentPacket = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sentPacket = packet)
				.Returns(Task.Delay(0));

			var connectionProvider = new Mock<IConnectionProvider> ();

			connectionProvider
				.Setup (p => p.GetConnection (It.Is<string> (c => c == clientId)))
				.Returns (channel.Object);

			var flow = new ServerConnectFlow (authenticationProvider, sessionRepository.Object, willRepository.Object, senderFlow.Object);

			await flow.ExecuteAsync (clientId, connect, channel.Object)
				.ConfigureAwait(continueOnCapturedContext: false);

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
			var authenticationProvider = Mock.Of<IAuthenticationProvider> (p => p.Authenticate (It.IsAny<string> (), It.IsAny<string> ()) == true);
			var sessionRepository = new Mock<IRepository<ClientSession>> ();
			var willRepository = new Mock<IRepository<ConnectionWill>> ();

			var clientId = Guid.NewGuid ().ToString ();
			var existingSession = new ClientSession { ClientId = clientId, Clean = false };

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>>()))
				.Returns (existingSession);

			var senderFlow = new Mock<IPublishSenderFlow> ();

			var connect = new Connect (clientId, cleanSession: false);
			var channel = new Mock<IChannel<IPacket>> ();
			var sentPacket = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sentPacket = packet)
				.Returns(Task.Delay(0));

			var connectionProvider = new Mock<IConnectionProvider> ();

			connectionProvider
				.Setup (p => p.GetConnection (It.Is<string> (c => c == clientId)))
				.Returns (channel.Object);

			var flow = new ServerConnectFlow (authenticationProvider, sessionRepository.Object, willRepository.Object, senderFlow.Object);

			await flow.ExecuteAsync (clientId, connect, channel.Object)
				.ConfigureAwait(continueOnCapturedContext: false);

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
			var authenticationProvider = Mock.Of<IAuthenticationProvider> (p => p.Authenticate (It.IsAny<string> (), It.IsAny<string> ()) == true);
			var sessionRepository = new Mock<IRepository<ClientSession>> ();
			var willRepository = new Mock<IRepository<ConnectionWill>> ();

			var clientId = Guid.NewGuid ().ToString ();
			var existingSession = new ClientSession { ClientId = clientId, Clean = true };

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>>()))
				.Returns (existingSession);

			var senderFlow = new Mock<IPublishSenderFlow> ();

			var connect = new Connect (clientId, cleanSession: true);
			var channel = new Mock<IChannel<IPacket>> ();
			var sentPacket = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sentPacket = packet)
				.Returns(Task.Delay(0));

			var connectionProvider = new Mock<IConnectionProvider> ();

			connectionProvider
				.Setup (p => p.GetConnection (It.Is<string> (c => c == clientId)))
				.Returns (channel.Object);

			var flow = new ServerConnectFlow (authenticationProvider, sessionRepository.Object, willRepository.Object, senderFlow.Object);

			await flow.ExecuteAsync (clientId, connect, channel.Object)
				.ConfigureAwait(continueOnCapturedContext: false);

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
			var authenticationProvider = Mock.Of<IAuthenticationProvider> (p => p.Authenticate (It.IsAny<string> (), It.IsAny<string> ()) == true);
			var sessionRepository = new Mock<IRepository<ClientSession>> ();
			var willRepository = new Mock<IRepository<ConnectionWill>> ();

			var clientId = Guid.NewGuid ().ToString ();

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>>()))
				.Returns (default(ClientSession));

			var senderFlow = new Mock<IPublishSenderFlow> ();

			var connect = new Connect (clientId, cleanSession: false);
			var channel = new Mock<IChannel<IPacket>> ();
			var sentPacket = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sentPacket = packet)
				.Returns(Task.Delay(0));

			var connectionProvider = new Mock<IConnectionProvider> ();

			connectionProvider
				.Setup (p => p.GetConnection (It.Is<string> (c => c == clientId)))
				.Returns (channel.Object);

			var flow = new ServerConnectFlow (authenticationProvider, sessionRepository.Object, willRepository.Object, senderFlow.Object);

			await flow.ExecuteAsync (clientId, connect, channel.Object)
				.ConfigureAwait(continueOnCapturedContext: false);

			var connectAck = sentPacket as ConnectAck;

			Assert.False (connectAck.SessionPresent);
		}

		[Fact]
		public async Task when_sending_connect_with_will_then_will_is_created_and_ack_is_sent()
		{
			var authenticationProvider = Mock.Of<IAuthenticationProvider> (p => p.Authenticate (It.IsAny<string> (), It.IsAny<string> ()) == true);
			var sessionRepository = new Mock<IRepository<ClientSession>> ();
			var willRepository = new Mock<IRepository<ConnectionWill>> ();

			var senderFlow = new Mock<IPublishSenderFlow> ();

			var clientId = Guid.NewGuid ().ToString ();
			var connect = new Connect (clientId, cleanSession: true);

			var will = new Will ("foo/bar", QualityOfService.AtLeastOnce, retain: true, message: "Foo Will Message");

			connect.Will = will;

			var channel = new Mock<IChannel<IPacket>> ();
			var sentPacket = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sentPacket = packet)
				.Returns(Task.Delay(0));

			var connectionProvider = new Mock<IConnectionProvider> ();

			connectionProvider
				.Setup (p => p.GetConnection (It.Is<string> (c => c == clientId)))
				.Returns (channel.Object);

			var flow = new ServerConnectFlow (authenticationProvider, sessionRepository.Object, willRepository.Object, senderFlow.Object);

			await flow.ExecuteAsync (clientId, connect, channel.Object)
				.ConfigureAwait(continueOnCapturedContext: false);

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
		public void when_sending_connect_with_invalid_user_credentials_then_connection_exception_is_thrown()
		{
			var authenticationProvider = Mock.Of<IAuthenticationProvider> (p => p.Authenticate (It.IsAny<string> (), It.IsAny<string> ()) == false);
			var sessionRepository = new Mock<IRepository<ClientSession>> ();
			var willRepository = new Mock<IRepository<ConnectionWill>> ();
			var senderFlow = new Mock<IPublishSenderFlow> ();

			var clientId = Guid.NewGuid ().ToString ();
			var connect = new Connect (clientId, cleanSession: true);
			var channel = new Mock<IChannel<IPacket>> ();
			var sentPacket = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sentPacket = packet)
				.Returns(Task.Delay(0));

			var connectionProvider = new Mock<IConnectionProvider> ();

			connectionProvider
				.Setup (p => p.GetConnection (It.Is<string> (c => c == clientId)))
				.Returns (channel.Object);

			var flow = new ServerConnectFlow (authenticationProvider, sessionRepository.Object, willRepository.Object, senderFlow.Object);

			var aggregateEx = Assert.Throws<AggregateException>(() => flow.ExecuteAsync (clientId, connect, channel.Object).Wait());

			Assert.NotNull (aggregateEx.InnerException);
			Assert.True (aggregateEx.InnerException is ProtocolConnectionException);
			Assert.Equal (ConnectionStatus.BadUserNameOrPassword, ((ProtocolConnectionException)aggregateEx.InnerException).ReturnCode);
		}
	}
}
