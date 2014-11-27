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
	public class DisconnectFlowSpec
	{
		[Fact]
		public async Task when_sending_disconnect_then_succeeds()
		{
			var clientManager = new Mock<IClientManager> ();
			var willRepository = new Mock<IRepository<ConnectionWill>> ();

			var flow = new DisconnectFlow (clientManager.Object, willRepository.Object);

			var clientId = Guid.NewGuid ().ToString ();
			var channel = new Mock<IChannel<IPacket>> ();
			var disconnect = new Disconnect ();

			await flow.ExecuteAsync (clientId, disconnect, channel.Object);

			clientManager.Verify (m => m.RemoveClient (It.Is<string> (s => s == clientId)));
			willRepository.Verify (r => r.Delete (It.IsAny<Expression<Func<ConnectionWill, bool>>> ()));
			channel.Verify (c => c.Close ());
		}
	}
}
