using System.Collections.Generic;
using Hermes.Packets;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class ServerProtocolFlowProvider : ProtocolFlowProvider
	{
		readonly IConnectionProvider connectionProvider;
		readonly IPublishDispatcher publishDispatcher;

		public ServerProtocolFlowProvider (IConnectionProvider connectionProvider,
 			IPublishDispatcher publishDispatcher,
			ITopicEvaluator topicEvaluator,
			IRepositoryFactory repositoryFactory, 
			ProtocolConfiguration configuration)
			: base(topicEvaluator, repositoryFactory, configuration)
		{
			this.connectionProvider = connectionProvider;
			this.publishDispatcher = publishDispatcher;
		}

		protected override IDictionary<ProtocolFlowType, IProtocolFlow> GetFlows ()
		{
			var flows = new Dictionary<ProtocolFlowType, IProtocolFlow>();

			var sessionRepository = repositoryFactory.CreateRepository<ClientSession>();
			var willRepository = repositoryFactory.CreateRepository<ConnectionWill> ();
			var retainedRepository = repositoryFactory.CreateRepository<RetainedMessage> ();
			var packetIdentifierRepository = repositoryFactory.CreateRepository<PacketIdentifier> ();

			var senderFlow = new PublishSenderFlow (sessionRepository, packetIdentifierRepository, configuration);

			flows.Add (ProtocolFlowType.Connect, new ServerConnectFlow (sessionRepository, willRepository, 
				packetIdentifierRepository, senderFlow));
			flows.Add (ProtocolFlowType.PublishSender, senderFlow);
			flows.Add (ProtocolFlowType.PublishReceiver, new ServerPublishReceiverFlow (topicEvaluator, 
				retainedRepository, sessionRepository, packetIdentifierRepository, this.publishDispatcher, configuration));
			flows.Add (ProtocolFlowType.Subscribe, new ServerSubscribeFlow (topicEvaluator, sessionRepository, 
				packetIdentifierRepository, retainedRepository, senderFlow, configuration));
			flows.Add (ProtocolFlowType.Unsubscribe, new ServerUnsubscribeFlow (sessionRepository, packetIdentifierRepository));
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
