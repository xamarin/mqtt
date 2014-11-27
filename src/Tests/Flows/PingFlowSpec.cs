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
			var flow = new PingFlow ();

			var clientId = Guid.NewGuid ().ToString ();
			var channel = new Mock<IChannel<IPacket>> ();
			var sentPacket = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sentPacket = packet)
				.Returns(Task.Delay(0));

			await flow.ExecuteAsync (clientId, new PingRequest(), channel.Object);

			var pingResponse = sentPacket as PingResponse;

			Assert.NotNull (pingResponse);
			Assert.Equal (PacketType.PingResponse, pingResponse.Type);
		}
	}
}
