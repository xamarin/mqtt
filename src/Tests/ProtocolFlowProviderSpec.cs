using System;
using System.Reactive;
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
		[InlineData(PacketType.Publish, typeof(PublishReceiverFlow))]
		[InlineData(PacketType.PublishAck, typeof(PublishSenderFlow))]
		[InlineData(PacketType.PublishReceived, typeof(PublishSenderFlow))]
		[InlineData(PacketType.PublishRelease, typeof(PublishReceiverFlow))]
		[InlineData(PacketType.PublishComplete, typeof(PublishSenderFlow))]
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
		[InlineData(PacketType.Publish, typeof(ServerPublishReceiverFlow))]
		[InlineData(PacketType.PublishAck, typeof(PublishSenderFlow))]
		[InlineData(PacketType.PublishReceived, typeof(PublishSenderFlow))]
		[InlineData(PacketType.PublishRelease, typeof(ServerPublishReceiverFlow))]
		[InlineData(PacketType.PublishComplete, typeof(PublishSenderFlow))]
		[InlineData(PacketType.Subscribe, typeof(ServerSubscribeFlow))]
		[InlineData(PacketType.Unsubscribe, typeof(ServerUnsubscribeFlow))]
		public void when_getting_server_flow_from_valid_packet_type_then_succeeds(PacketType packetType, Type flowType)
		{
			var flowProvider = new ServerProtocolFlowProvider (Mock.Of<IConnectionProvider> (), Mock.Of<ITopicEvaluator> (), 
				Mock.Of<IRepositoryProvider>(), new EventStream(), new ProtocolConfiguration ());

			var flow = flowProvider.GetFlow (packetType);

			Assert.Equal (flowType, flow.GetType ());
		}

		[Fact]
		public void when_getting_explicit_server_flow_from_type_then_succeeds()
		{
			var flowProvider = new ServerProtocolFlowProvider (Mock.Of<IConnectionProvider> (), Mock.Of<ITopicEvaluator> (), 
				Mock.Of<IRepositoryProvider>(), new EventStream(), new ProtocolConfiguration ());

			var connectFlow = flowProvider.GetFlow<ServerConnectFlow> ();
			var senderFlow = flowProvider.GetFlow<PublishSenderFlow> ();
			var receiverFlow = flowProvider.GetFlow<ServerPublishReceiverFlow> ();
			var subscribeFlow = flowProvider.GetFlow<ServerSubscribeFlow> ();
			var unsubscribeFlow = flowProvider.GetFlow<ServerUnsubscribeFlow> ();
			var disconnectFlow = flowProvider.GetFlow<DisconnectFlow> ();

			Assert.NotNull (connectFlow);
			Assert.NotNull (senderFlow);
			Assert.NotNull (receiverFlow);
			Assert.NotNull (subscribeFlow);
			Assert.NotNull (unsubscribeFlow);
			Assert.NotNull (disconnectFlow);
		}

		[Fact]
		public void when_getting_explicit_client_flow_from_type_then_succeeds()
		{
			var flowProvider = new ClientProtocolFlowProvider (Mock.Of<ITopicEvaluator> (), Mock.Of<IRepositoryProvider>(), new ProtocolConfiguration ());

			var connectFlow = flowProvider.GetFlow<ClientConnectFlow> ();
			var senderFlow = flowProvider.GetFlow<PublishSenderFlow> ();
			var receiverFlow = flowProvider.GetFlow<PublishReceiverFlow> ();
			var subscribeFlow = flowProvider.GetFlow<ClientSubscribeFlow> ();
			var unsubscribeFlow = flowProvider.GetFlow<ClientUnsubscribeFlow> ();
			var disconnectFlow = flowProvider.GetFlow<PingFlow> ();

			Assert.NotNull (connectFlow);
			Assert.NotNull (senderFlow);
			Assert.NotNull (receiverFlow);
			Assert.NotNull (subscribeFlow);
			Assert.NotNull (unsubscribeFlow);
			Assert.NotNull (disconnectFlow);
		}
	}
}
