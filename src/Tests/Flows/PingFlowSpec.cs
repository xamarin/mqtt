using System;
using System.Threading.Tasks;
using Hermes;
using Hermes.Flows;
using Hermes.Packets;
using Moq;
using Xunit;

namespace Tests.Flows
{
	public class PingFlowSpec
	{
		[Fact]
		public async Task when_sending_ping_request_then_ping_response_is_sent()
		{
			var clientId = Guid.NewGuid ().ToString ();
			var channel = new Mock<IChannel<IPacket>> ();
			var sentPacket = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sentPacket = packet)
				.Returns(Task.Delay(0));

			var connectionProvider = new Mock<IConnectionProvider> ();

			connectionProvider
				.Setup (p => p.GetConnection (It.Is<string> (c => c == clientId)))
				.Returns (channel.Object);

			var flow = new PingFlow (connectionProvider.Object);

			await flow.ExecuteAsync (clientId, new PingRequest());

			var pingResponse = sentPacket as PingResponse;

			Assert.NotNull (pingResponse);
			Assert.Equal (PacketType.PingResponse, pingResponse.Type);
		}
	}
}
