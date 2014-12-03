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
		[InlineData(PacketType.ConnectAck, typeof(ClientConnectFlow))]
		[InlineData(PacketType.PingResponse, typeof(PingFlow))]
		[InlineData(PacketType.Publish, typeof(PublishSenderFlow))]
		[InlineData(PacketType.PublishAck, typeof(PublishReceiverFlow))]
		[InlineData(PacketType.PublishReceived, typeof(PublishReceiverFlow))]
		[InlineData(PacketType.PublishRelease, typeof(PublishSenderFlow))]
		[InlineData(PacketType.PublishComplete, typeof(PublishReceiverFlow))]
		[InlineData(PacketType.SubscribeAck, typeof(ClientSubscribeFlow))]
		[InlineData(PacketType.UnsubscribeAck, typeof(ClientUnsubscribeFlow))]
		public void when_getting_client_flow_from_valid_packet_type_then_succeeds(PacketType packetType, Type flowType)
		{
			var flowProvider = new ClientProtocolFlowProvider (Mock.Of<ITopicEvaluator> (), Mock.Of<IRepositoryProvider>(), new ProtocolConfiguration ());

			var flow = flowProvider.GetFlow (packetType);

			Assert.Equal (flowType, flow.GetType ());
		}

		[Theory]
		[InlineData(PacketType.Connect, typeof(ServerConnectFlow))]
		[InlineData(PacketType.Disconnect, typeof(DisconnectFlow))]
		[InlineData(PacketType.PingRequest, typeof(PingFlow))]
		[InlineData(PacketType.Publish, typeof(PublishSenderFlow))]
		[InlineData(PacketType.PublishAck, typeof(ServerPublishReceiverFlow))]
		[InlineData(PacketType.PublishReceived, typeof(ServerPublishReceiverFlow))]
		[InlineData(PacketType.PublishRelease, typeof(PublishSenderFlow))]
		[InlineData(PacketType.PublishComplete, typeof(ServerPublishReceiverFlow))]
		[InlineData(PacketType.Subscribe, typeof(ServerSubscribeFlow))]
		[InlineData(PacketType.Unsubscribe, typeof(ServerUnsubscribeFlow))]
		public void when_getting_server_flow_from_valid_packet_type_then_succeeds(PacketType packetType, Type flowType)
		{
			var flowProvider = new ServerProtocolFlowProvider (Mock.Of<IConnectionProvider> (), Mock.Of<ITopicEvaluator> (), 
				Mock.Of<IRepositoryProvider>(), new ProtocolConfiguration ());

			var flow = flowProvider.GetFlow (packetType);

			Assert.Equal (flowType, flow.GetType ());
		}
	}
}
