using Moq;
using System;
using System.Net.Mqtt;
using System.Net.Mqtt.Sdk.Flows;
using System.Net.Mqtt.Sdk.Packets;
using System.Net.Mqtt.Sdk.Storage;
using System.Reactive.Subjects;
using Xunit;
using Xunit.Extensions;
using System.Net.Mqtt.Sdk;

namespace Tests
{
    internal class ProtocolFlowProviderSpec
	{
		[Theory]
		[InlineData(MqttPacketType.ConnectAck, typeof(ClientConnectFlow))]
		[InlineData(MqttPacketType.PingResponse, typeof(PingFlow))]
		[InlineData(MqttPacketType.Publish, typeof(PublishReceiverFlow))]
		[InlineData(MqttPacketType.PublishAck, typeof(PublishSenderFlow))]
		[InlineData(MqttPacketType.PublishReceived, typeof(PublishSenderFlow))]
		[InlineData(MqttPacketType.PublishRelease, typeof(PublishReceiverFlow))]
		[InlineData(MqttPacketType.PublishComplete, typeof(PublishSenderFlow))]
		[InlineData(MqttPacketType.SubscribeAck, typeof(ClientSubscribeFlow))]
		[InlineData(MqttPacketType.UnsubscribeAck, typeof(ClientUnsubscribeFlow))]
		public void when_getting_client_flow_from_valid_packet_type_then_succeeds(MqttPacketType packetType, Type flowType)
		{
			var flowProvider = new ClientProtocolFlowProvider (Mock.Of<IMqttTopicEvaluator> (), Mock.Of<IRepositoryProvider>(), new MqttConfiguration ());

			var flow = flowProvider.GetFlow (packetType);

			Assert.Equal (flowType, flow.GetType ());
		}

		[Theory]
		[InlineData(MqttPacketType.Connect, typeof(ServerConnectFlow))]
		[InlineData(MqttPacketType.Disconnect, typeof(DisconnectFlow))]
		[InlineData(MqttPacketType.PingRequest, typeof(PingFlow))]
		[InlineData(MqttPacketType.Publish, typeof(ServerPublishReceiverFlow))]
		[InlineData(MqttPacketType.PublishAck, typeof(PublishSenderFlow))]
		[InlineData(MqttPacketType.PublishReceived, typeof(PublishSenderFlow))]
		[InlineData(MqttPacketType.PublishRelease, typeof(ServerPublishReceiverFlow))]
		[InlineData(MqttPacketType.PublishComplete, typeof(PublishSenderFlow))]
		[InlineData(MqttPacketType.Subscribe, typeof(ServerSubscribeFlow))]
		[InlineData(MqttPacketType.Unsubscribe, typeof(ServerUnsubscribeFlow))]
		public void when_getting_server_flow_from_valid_packet_type_then_succeeds(MqttPacketType packetType, Type flowType)
		{
			var authenticationProvider = Mock.Of<IMqttAuthenticationProvider> (p => p.Authenticate (It.IsAny<string>(), It.IsAny<string> (), It.IsAny<string> ()) == true);
			var flowProvider = new ServerProtocolFlowProvider (authenticationProvider, Mock.Of<IConnectionProvider> (), Mock.Of<IMqttTopicEvaluator> (), 
				Mock.Of<IRepositoryProvider>(), Mock.Of<IPacketIdProvider>(), Mock.Of<ISubject<MqttUndeliveredMessage>> (), new MqttConfiguration ());

			var flow = flowProvider.GetFlow (packetType);

			Assert.Equal (flowType, flow.GetType ());
		}

		[Fact]
		public void when_getting_explicit_server_flow_from_type_then_succeeds()
		{
			var authenticationProvider = Mock.Of<IMqttAuthenticationProvider> (p => p.Authenticate (It.IsAny<string>(), It.IsAny<string> (), It.IsAny<string> ()) == true);
			var flowProvider = new ServerProtocolFlowProvider (authenticationProvider, Mock.Of<IConnectionProvider> (), Mock.Of<IMqttTopicEvaluator> (), 
				Mock.Of<IRepositoryProvider>(), Mock.Of<IPacketIdProvider>(), Mock.Of<ISubject<MqttUndeliveredMessage>> (), new MqttConfiguration ());

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
			var flowProvider = new ClientProtocolFlowProvider (Mock.Of<IMqttTopicEvaluator> (), Mock.Of<IRepositoryProvider>(), new MqttConfiguration ());

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
