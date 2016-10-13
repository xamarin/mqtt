using System.Collections.Generic;
using System.Net.Mqtt.Sdk.Packets;
using System.Net.Mqtt.Sdk.Storage;
using System.Reactive.Subjects;

namespace System.Net.Mqtt.Sdk.Flows
{
    internal class ServerProtocolFlowProvider : ProtocolFlowProvider
	{
		readonly IMqttAuthenticationProvider authenticationProvider;
		readonly IConnectionProvider connectionProvider;
		readonly IPacketIdProvider packetIdProvider;
		readonly ISubject<MqttUndeliveredMessage> undeliveredMessagesListener;

		public ServerProtocolFlowProvider (IMqttAuthenticationProvider authenticationProvider,
			IConnectionProvider connectionProvider,
			IMqttTopicEvaluator topicEvaluator,
			IRepositoryProvider repositoryProvider,
			IPacketIdProvider packetIdProvider,
            ISubject<MqttUndeliveredMessage> undeliveredMessagesListener,
			MqttConfiguration configuration)
			: base (topicEvaluator, repositoryProvider, configuration)
		{
			this.authenticationProvider = authenticationProvider;
			this.connectionProvider = connectionProvider;
			this.packetIdProvider = packetIdProvider;
			this.undeliveredMessagesListener = undeliveredMessagesListener;
		}

		protected override IDictionary<ProtocolFlowType, IProtocolFlow> InitializeFlows ()
		{
			var flows = new Dictionary<ProtocolFlowType, IProtocolFlow>();

			var sessionRepository = repositoryProvider.GetRepository<ClientSession>();
			var willRepository = repositoryProvider.GetRepository<ConnectionWill> ();
			var retainedRepository = repositoryProvider.GetRepository<RetainedMessage> ();
			var senderFlow = new PublishSenderFlow (sessionRepository, configuration);

			flows.Add (ProtocolFlowType.Connect, new ServerConnectFlow (authenticationProvider, sessionRepository, willRepository, senderFlow));
			flows.Add (ProtocolFlowType.PublishSender, senderFlow);
			flows.Add (ProtocolFlowType.PublishReceiver, new ServerPublishReceiverFlow (topicEvaluator, connectionProvider,
				senderFlow, retainedRepository, sessionRepository, willRepository, packetIdProvider, undeliveredMessagesListener, configuration));
			flows.Add (ProtocolFlowType.Subscribe, new ServerSubscribeFlow (topicEvaluator, sessionRepository,
				retainedRepository, packetIdProvider, senderFlow, configuration));
			flows.Add (ProtocolFlowType.Unsubscribe, new ServerUnsubscribeFlow (sessionRepository));
			flows.Add (ProtocolFlowType.Ping, new PingFlow ());
			flows.Add (ProtocolFlowType.Disconnect, new DisconnectFlow (connectionProvider, sessionRepository, willRepository));

			return flows;
		}

		protected override bool IsValidPacketType (MqttPacketType packetType)
		{
			return packetType == MqttPacketType.Connect ||
				packetType == MqttPacketType.Subscribe ||
				packetType == MqttPacketType.Unsubscribe ||
				packetType == MqttPacketType.Publish ||
				packetType == MqttPacketType.PublishAck ||
				packetType == MqttPacketType.PublishComplete ||
				packetType == MqttPacketType.PublishReceived ||
				packetType == MqttPacketType.PublishRelease ||
				packetType == MqttPacketType.PingRequest ||
				packetType == MqttPacketType.Disconnect;
		}
	}
}
