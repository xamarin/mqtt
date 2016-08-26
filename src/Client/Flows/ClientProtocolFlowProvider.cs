using System.Collections.Generic;
using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Storage;

namespace System.Net.Mqtt.Flows
{
	internal class ClientProtocolFlowProvider : ProtocolFlowProvider
	{
		public ClientProtocolFlowProvider (IMqttTopicEvaluator topicEvaluator, 
			IRepositoryProvider repositoryProvider, 
			ITracerManager tracerManager,
			MqttConfiguration configuration)
			: base (topicEvaluator, repositoryProvider, tracerManager, configuration)
		{
		}

		protected override IDictionary<ProtocolFlowType, IProtocolFlow> InitializeFlows ()
		{
			var flows = new Dictionary<ProtocolFlowType, IProtocolFlow>();

			var sessionRepository = repositoryProvider.GetRepository<ClientSession>();
			var retainedRepository = repositoryProvider.GetRepository<RetainedMessage> ();
			var senderFlow = new PublishSenderFlow (sessionRepository, tracerManager, configuration);

			flows.Add (ProtocolFlowType.Connect, new ClientConnectFlow (sessionRepository, senderFlow));
			flows.Add (ProtocolFlowType.PublishSender, senderFlow);
			flows.Add (ProtocolFlowType.PublishReceiver, new PublishReceiverFlow (topicEvaluator,
				retainedRepository, sessionRepository, tracerManager, configuration));
			flows.Add (ProtocolFlowType.Subscribe, new ClientSubscribeFlow ());
			flows.Add (ProtocolFlowType.Unsubscribe, new ClientUnsubscribeFlow ());
			flows.Add (ProtocolFlowType.Ping, new PingFlow ());

			return flows;
		}

		protected override bool IsValidPacketType (MqttPacketType packetType)
		{
			return packetType == MqttPacketType.ConnectAck ||
				packetType == MqttPacketType.SubscribeAck ||
				packetType == MqttPacketType.UnsubscribeAck ||
				packetType == MqttPacketType.Publish ||
				packetType == MqttPacketType.PublishAck ||
				packetType == MqttPacketType.PublishComplete ||
				packetType == MqttPacketType.PublishReceived ||
				packetType == MqttPacketType.PublishRelease ||
				packetType == MqttPacketType.PingResponse;
		}
	}
}
