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
			var context = new Mock<ICommunicationContext> ();
			var disconnect = new Disconnect ();

			await flow.ExecuteAsync (clientId, disconnect, context.Object);

			clientManager.Verify (m => m.RemoveClient (It.Is<string> (s => s == clientId)));
			willRepository.Verify (r => r.Delete (It.IsAny<Expression<Func<ConnectionWill, bool>>> ()));
			context.Verify (c => c.Dispose ());
		}

		[Fact]
		public void when_sending_invalid_packet_to_disconnect_then_fails()
		{
				var clientManager = new Mock<IClientManager> ();
			var willRepository = new Mock<IRepository<ConnectionWill>> ();

			var flow = new DisconnectFlow (clientManager.Object, willRepository.Object);

			var clientId = Guid.NewGuid ().ToString ();
			var invalid = new Connect (clientId, cleanSession: true);
			var context = new Mock<ICommunicationContext> ();
			var sentPacket = default(IPacket);

			context.Setup (c => c.PushDeliveryAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sentPacket = packet)
				.Returns(Task.Delay(0));

			Assert.Throws<ProtocolException> (() => flow.ExecuteAsync (clientId, invalid, context.Object).Wait());
		}
	}
}
