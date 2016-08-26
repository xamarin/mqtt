using System;
using System.Threading.Tasks;
using System.Net.Mqtt;
using System.Net.Mqtt.Flows;
using System.Net.Mqtt.Packets;
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

			var flow = new PingFlow ();

			await flow.ExecuteAsync (clientId, new PingRequest(), channel.Object)
				.ConfigureAwait(continueOnCapturedContext: false);

			var pingResponse = sentPacket as PingResponse;

			Assert.NotNull (pingResponse);
			Assert.Equal (PacketType.PingResponse, pingResponse.Type);
		}
	}
}
