using Moq;
using System;
using System.Net.Mqtt.Sdk;
using System.Net.Mqtt.Sdk.Flows;
using System.Net.Mqtt.Sdk.Packets;
using System.Net.Mqtt.Sdk.Storage;
using System.Threading.Tasks;
using Xunit;

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

			var session = new ClientSession (clientId, clean: true);

			connectionProvider
				.Setup (p => p.GetConnectionAsync (It.Is<string> (c => c == clientId)))
				.Returns (Task.FromResult(channel.Object));

			sessionRepository.Setup(r => r.Read(It.IsAny<string>())).Returns(session);

			await flow.ExecuteAsync (clientId, disconnect, channel.Object)
				.ConfigureAwait(continueOnCapturedContext: false);

			willRepository.Verify (r => r.Delete (It.IsAny<string> ()));
			sessionRepository.Verify(r => r.Delete(It.Is<string>(s => s == session.Id)));
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

			var session = new ClientSession(clientId, clean: false);

			connectionProvider
				.Setup (p => p.GetConnectionAsync (It.Is<string> (c => c == clientId)))
				.Returns (Task.FromResult(channel.Object));

			sessionRepository.Setup(r => r.Read(It.IsAny<string>())).Returns(session);

			await flow.ExecuteAsync (clientId, disconnect, channel.Object)
				.ConfigureAwait(continueOnCapturedContext: false);

			willRepository.Verify (r => r.Delete (It.IsAny<string> ()));
			sessionRepository.Verify(r => r.Delete(It.Is<string>(s => s == session.Id)), Times.Never);
		}
	}
}
