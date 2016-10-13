using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Net.Mqtt.Sdk.Flows;
using System.Net.Mqtt.Sdk.Packets;
using System.Net.Mqtt.Sdk.Storage;
using Moq;
using Xunit;
using System.Net.Mqtt.Sdk;

namespace Tests.Flows
{
	public class DisconnectFlowSpec
	{
		[Fact]
		public async Task when_sending_disconnect_and_client_session_has_clean_state_then_disconnects_and_delete_will_and_session()
		{
			var connectionProvider = new Mock<IConnectionProvider> ();
			var sessionRepository = new Mock<IRepository<ClientSession>>();
			var willRepository = new Mock<IRepository<ConnectionWill>> ();

			var flow = new DisconnectFlow (connectionProvider.Object, sessionRepository.Object, willRepository.Object);

			var clientId = Guid.NewGuid ().ToString ();
			var channel = new Mock<IMqttChannel<IPacket>> ();
			var disconnect = new Disconnect ();

			var session = new ClientSession
			{
				ClientId = clientId,
				Clean = true
			};

			connectionProvider
				.Setup (p => p.GetConnection (It.Is<string> (c => c == clientId)))
				.Returns (channel.Object);

			sessionRepository.Setup(r => r.Get(It.IsAny<Expression<Func<ClientSession, bool>>>())).Returns(session);

			await flow.ExecuteAsync (clientId, disconnect, channel.Object)
				.ConfigureAwait(continueOnCapturedContext: false);

			willRepository.Verify (r => r.Delete (It.IsAny<Expression<Func<ConnectionWill, bool>>> ()));
			sessionRepository.Verify(r => r.Delete(It.Is<ClientSession>(s => s == session)));
		}

		[Fact]
		public async Task when_sending_disconnect_and_client_session_has_persistent_state_then_disconnects_and_preserves_session()
		{
			var connectionProvider = new Mock<IConnectionProvider> ();
			var sessionRepository = new Mock<IRepository<ClientSession>>();
			var willRepository = new Mock<IRepository<ConnectionWill>> ();

			var flow = new DisconnectFlow (connectionProvider.Object, sessionRepository.Object, willRepository.Object);

			var clientId = Guid.NewGuid ().ToString ();
			var channel = new Mock<IMqttChannel<IPacket>> ();
			var disconnect = new Disconnect ();

			var session = new ClientSession
			{
				ClientId = clientId,
				Clean = false
			};

			connectionProvider
				.Setup (p => p.GetConnection (It.Is<string> (c => c == clientId)))
				.Returns (channel.Object);

			sessionRepository.Setup(r => r.Get(It.IsAny<Expression<Func<ClientSession, bool>>>())).Returns(session);

			await flow.ExecuteAsync (clientId, disconnect, channel.Object)
				.ConfigureAwait(continueOnCapturedContext: false);

			willRepository.Verify (r => r.Delete (It.IsAny<Expression<Func<ConnectionWill, bool>>> ()));
			sessionRepository.Verify(r => r.Delete(It.Is<ClientSession>(s => s == session)), Times.Never);
		}
	}
}
