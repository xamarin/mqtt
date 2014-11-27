using System.Collections.Generic;
using Hermes.Packets;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class ServerProtocolFlowProvider : ProtocolFlowProvider
	{
		public ServerProtocolFlowProvider (IRepositoryFactory repositoryFactory, ProtocolConfiguration configuration)
			: this(new ConnectionProvider(), new TopicEvaluator(configuration), repositoryFactory, configuration)
		{
		}

		public ServerProtocolFlowProvider (IConnectionProvider connectionProvider, ITopicEvaluator topicEvaluator,
			IRepositoryFactory repositoryFactory, ProtocolConfiguration configuration)
			: base(connectionProvider, topicEvaluator, repositoryFactory, configuration)
		{
		}

		protected override IDictionary<ProtocolFlowType, IProtocolFlow> GetFlows ()
		{
			var flows = new Dictionary<ProtocolFlowType, IProtocolFlow>();

			var sessionRepository = repositoryFactory.CreateRepository<ClientSession>();
			var willRepository = repositoryFactory.CreateRepository<ConnectionWill> ();
			var retainedRepository = repositoryFactory.CreateRepository<RetainedMessage> ();
			var packetIdentifierRepository = repositoryFactory.CreateRepository<PacketIdentifier> ();

			var senderFlow = new PublishSenderFlow (connectionProvider,
				sessionRepository, packetIdentifierRepository, configuration);

			flows.Add (ProtocolFlowType.Connect, new ServerConnectFlow (sessionRepository, willRepository, packetIdentifierRepository, senderFlow));
			flows.Add (ProtocolFlowType.PublishSender, senderFlow);
			flows.Add (ProtocolFlowType.PublishReceiver, new ServerPublishReceiverFlow (connectionProvider, topicEvaluator, 
				retainedRepository, sessionRepository, packetIdentifierRepository, senderFlow, configuration));
			flows.Add (ProtocolFlowType.Subscribe, new ServerSubscribeFlow (topicEvaluator, sessionRepository, packetIdentifierRepository, 
				retainedRepository, senderFlow, configuration));
			flows.Add (ProtocolFlowType.Unsubscribe, new ServerUnsubscribeFlow (sessionRepository, packetIdentifierRepository));
			flows.Add (ProtocolFlowType.Ping, new PingFlow ());
			flows.Add (ProtocolFlowType.Disconnect, new DisconnectFlow (connectionProvider, sessionRepository, willRepository));

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
