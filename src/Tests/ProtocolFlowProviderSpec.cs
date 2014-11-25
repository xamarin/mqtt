using System;
using Hermes;
using Hermes.Flows;
using Hermes.Packets;
using Hermes.Storage;
using Moq;
using Xunit;
using Xunit.Extensions;

namespace Tests
{
	public class ProtocolFlowProviderSpec
	{
		[Theory]
		[InlineData(PacketType.Connect, typeof(ConnectFlow))]
		[InlineData(PacketType.ConnectAck, typeof(ConnectFlow))]
		[InlineData(PacketType.Disconnect, typeof(DisconnectFlow))]
		[InlineData(PacketType.PingRequest, typeof(PingFlow))]
		[InlineData(PacketType.PingResponse, typeof(PingFlow))]
		[InlineData(PacketType.Publish, typeof(PublishFlow))]
		[InlineData(PacketType.PublishAck, typeof(PublishFlow))]
		[InlineData(PacketType.PublishReceived, typeof(PublishFlow))]
		[InlineData(PacketType.PublishRelease, typeof(PublishFlow))]
		[InlineData(PacketType.PublishComplete, typeof(PublishFlow))]
		[InlineData(PacketType.Subscribe, typeof(SubscribeFlow))]
		[InlineData(PacketType.SubscribeAck, typeof(SubscribeFlow))]
		[InlineData(PacketType.Unsubscribe, typeof(UnsubscribeFlow))]
		[InlineData(PacketType.UnsubscribeAck, typeof(UnsubscribeFlow))]
		public void when_getting_flow_from_valid_packet_type_then_succeeds(PacketType packetType, Type flowType)
		{
			var flowProvider = new ProtocolFlowProvider (Mock.Of<IClientManager> (),
				Mock.Of<ITopicEvaluator> (), Mock.Of<IRepository<ClientSession>> (),
				Mock.Of<IRepository<RetainedMessage>> (), Mock.Of<IRepository<ConnectionWill>> (),
				Mock.Of<IRepository<PacketIdentifier>> (), new ProtocolConfiguration ());

			var flow = flowProvider.GetFlow (packetType);

			Assert.Equal (flowType, flow.GetType ());
		}
	}
}
