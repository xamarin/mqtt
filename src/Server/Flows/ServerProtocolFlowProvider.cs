using System.Collections.Generic;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class ServerProtocolFlowProvider : ProtocolFlowProvider
	{
		public ServerProtocolFlowProvider (IRepositoryFactory repositoryFactory, ProtocolConfiguration configuration)
			: this(new ClientManager(), new TopicEvaluator(configuration), repositoryFactory, configuration)
		{
		}

		public ServerProtocolFlowProvider (IClientManager clientManager, ITopicEvaluator topicEvaluator,
			IRepositoryFactory repositoryFactory, ProtocolConfiguration configuration)
			: base(clientManager, topicEvaluator, repositoryFactory, configuration)
		{
		}

		protected override IDictionary<ProtocolFlowType, IProtocolFlow> GetFlows ()
		{
			var flows = new Dictionary<ProtocolFlowType, IProtocolFlow>();

			var sessionRepository = repositoryFactory.CreateRepository<ClientSession>();
			var willRepository = repositoryFactory.CreateRepository<ConnectionWill> ();
			var retainedRepository = repositoryFactory.CreateRepository<RetainedMessage> ();
			var packetIdentifierRepository = repositoryFactory.CreateRepository<PacketIdentifier> ();

			var senderFlow = new PublishSenderFlow (configuration, clientManager,
				sessionRepository, packetIdentifierRepository);

			flows.Add (ProtocolFlowType.Connect, new ServerConnectFlow (sessionRepository, willRepository, packetIdentifierRepository, senderFlow));
			flows.Add (ProtocolFlowType.PublishSender, senderFlow);
			flows.Add (ProtocolFlowType.PublishReceiver, new PublishReceiverFlow (configuration, clientManager, topicEvaluator, 
				retainedRepository, sessionRepository, packetIdentifierRepository));
			flows.Add (ProtocolFlowType.Subscribe, new ServerSubscribeFlow (topicEvaluator, sessionRepository, packetIdentifierRepository, 
				retainedRepository, senderFlow, configuration));
			flows.Add (ProtocolFlowType.Unsubscribe, new ServerUnsubscribeFlow (sessionRepository, packetIdentifierRepository));
			flows.Add (ProtocolFlowType.Ping, new PingFlow ());
			flows.Add (ProtocolFlowType.Disconnect, new DisconnectFlow (clientManager, willRepository));

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
