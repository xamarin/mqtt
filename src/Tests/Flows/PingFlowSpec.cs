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
			var context = new Mock<ICommunicationContext> ();
			var sentPacket = default(IPacket);

			context.Setup (c => c.PushDeliveryAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sentPacket = packet)
				.Returns(Task.Delay(0));

			await flow.ExecuteAsync (clientId, new PingRequest(), context.Object);

			var pingResponse = sentPacket as PingResponse;

			Assert.NotNull (pingResponse);
			Assert.Equal (PacketType.PingResponse, pingResponse.Type);
		}

		[Fact]
		public void when_sending_invalid_packet_to_ping_then_fails()
		{
			var flow = new PingFlow ();

			var clientId = Guid.NewGuid ().ToString ();
			var context = new Mock<ICommunicationContext> ();
			var sentPacket = default(IPacket);

			context.Setup (c => c.PushDeliveryAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sentPacket = packet)
				.Returns(Task.Delay(0));

			var ex = Assert.Throws<AggregateException> (() => flow.ExecuteAsync (clientId, new Disconnect(), context.Object).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}
	}
}
