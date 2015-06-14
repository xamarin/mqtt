using System.Collections.Generic;
using System.Reactive;
using Hermes.Packets;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class ServerProtocolFlowProvider : ProtocolFlowProvider
	{
		readonly IAuthenticationProvider authenticationProvider;
		readonly IConnectionProvider connectionProvider;
		readonly IPacketIdProvider packetIdProvider;
		readonly IEventStream eventStream;

		public ServerProtocolFlowProvider (IAuthenticationProvider authenticationProvider,
			IConnectionProvider connectionProvider,
			ITopicEvaluator topicEvaluator,
			IRepositoryProvider repositoryProvider,
			IPacketIdProvider packetIdProvider,
			IEventStream eventStream,
			ProtocolConfiguration configuration)
			: base(topicEvaluator, repositoryProvider, configuration)
		{
			this.authenticationProvider = authenticationProvider;
			this.connectionProvider = connectionProvider;
			this.packetIdProvider = packetIdProvider;
			this.eventStream = eventStream;
		}

		protected override IDictionary<ProtocolFlowType, IProtocolFlow> InitializeFlows ()
		{
			var flows = new Dictionary<ProtocolFlowType, IProtocolFlow>();

			var sessionRepository = repositoryProvider.GetRepository<ClientSession>();
			var willRepository = repositoryProvider.GetRepository<ConnectionWill> ();
			var retainedRepository = repositoryProvider.GetRepository<RetainedMessage> ();
			var senderFlow = new PublishSenderFlow (sessionRepository, configuration);

			flows.Add (ProtocolFlowType.Connect, new ServerConnectFlow (this.authenticationProvider, sessionRepository, willRepository, senderFlow));
			flows.Add (ProtocolFlowType.PublishSender, senderFlow);
			flows.Add (ProtocolFlowType.PublishReceiver, new ServerPublishReceiverFlow (topicEvaluator, connectionProvider,
				senderFlow, retainedRepository, sessionRepository, willRepository, packetIdProvider, eventStream, configuration));
			flows.Add (ProtocolFlowType.Subscribe, new ServerSubscribeFlow (topicEvaluator, sessionRepository, 
				retainedRepository, packetIdProvider, senderFlow, configuration));
			flows.Add (ProtocolFlowType.Unsubscribe, new ServerUnsubscribeFlow (sessionRepository));
			flows.Add (ProtocolFlowType.Ping, new PingFlow ());
			flows.Add (ProtocolFlowType.Disconnect, new DisconnectFlow (this.connectionProvider, sessionRepository, willRepository));

			return flows;
		}

		protected override bool IsValidPacketType (PacketType packetType)
		{
			return packetType == PacketType.Connect ||
				packetType == PacketType.Subscribe ||
				packetType == PacketType.Unsubscribe ||
				packetType == PacketType.Publish ||
				packetType == PacketType.PublishAck ||
				packetType == PacketType.PublishComplete ||
				packetType == PacketType.PublishReceived ||
				packetType == PacketType.PublishRelease ||
				packetType == PacketType.PingRequest ||
				packetType == PacketType.Disconnect;
		}
	}
}
