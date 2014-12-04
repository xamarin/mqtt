using System.Collections.Generic;
using Hermes.Packets;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class ClientProtocolFlowProvider : ProtocolFlowProvider
	{
		public ClientProtocolFlowProvider (ITopicEvaluator topicEvaluator, IRepositoryProvider repositoryProvider, ProtocolConfiguration configuration)
			: base(topicEvaluator, repositoryProvider, configuration)
		{
		}

		protected override IDictionary<ProtocolFlowType, IProtocolFlow> InitializeFlows ()
		{
			var flows = new Dictionary<ProtocolFlowType, IProtocolFlow>();

			var sessionRepository = repositoryProvider.GetRepository<ClientSession>();
			var retainedRepository = repositoryProvider.GetRepository<RetainedMessage> ();
			var packetIdentifierRepository = repositoryProvider.GetRepository<PacketIdentifier> ();

			var senderFlow = new PublishSenderFlow (sessionRepository, packetIdentifierRepository,configuration);

			flows.Add (ProtocolFlowType.Connect, new ClientConnectFlow (sessionRepository, senderFlow));
			flows.Add (ProtocolFlowType.PublishSender, senderFlow);
			flows.Add (ProtocolFlowType.PublishReceiver, new PublishReceiverFlow (topicEvaluator, 
				retainedRepository, sessionRepository, packetIdentifierRepository, configuration));
			flows.Add (ProtocolFlowType.Subscribe, new ClientSubscribeFlow (packetIdentifierRepository));
			flows.Add (ProtocolFlowType.Unsubscribe, new ClientUnsubscribeFlow (packetIdentifierRepository));
			flows.Add (ProtocolFlowType.Ping, new PingFlow ());

			return flows;
		}

		protected override bool IsValidPacketType (PacketType packetType)
		{
			return packetType == PacketType.ConnectAck ||
				packetType == PacketType.SubscribeAck ||
				packetType == PacketType.UnsubscribeAck ||
				packetType == PacketType.Publish ||
				packetType == PacketType.PublishAck ||
				packetType == PacketType.PublishComplete ||
				packetType == PacketType.PublishReceived ||
				packetType == PacketType.PublishRelease ||
				packetType == PacketType.PingResponse;
		}
	}
}
