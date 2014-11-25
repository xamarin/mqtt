using System.Collections.Generic;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class ProtocolFlowProvider : IProtocolFlowProvider
	{
		readonly IDictionary<ProtocolFlowType, IProtocolFlow> flows;

		public ProtocolFlowProvider (IRepositoryFactory repositoryFactory, ProtocolConfiguration configuration)
			: this(new ClientManager(), new TopicEvaluator(configuration), repositoryFactory, configuration)
		{
		}

		public ProtocolFlowProvider (IClientManager clientManager, ITopicEvaluator topicEvaluator,
			IRepositoryFactory repositoryFactory, ProtocolConfiguration configuration)
		{
			this.flows = new Dictionary<ProtocolFlowType, IProtocolFlow>();

			var sessionRepository = repositoryFactory.CreateRepository<ClientSession>();
			var willRepository = repositoryFactory.CreateRepository<ConnectionWill> ();
			var retainedRepository = repositoryFactory.CreateRepository<RetainedMessage> ();
			var packetIdentifierRepository = repositoryFactory.CreateRepository<PacketIdentifier> ();

			this.flows.Add (ProtocolFlowType.Connect, new ConnectFlow (sessionRepository, willRepository));
			this.flows.Add (ProtocolFlowType.Publish, new PublishFlow (configuration, clientManager, topicEvaluator, retainedRepository, sessionRepository, packetIdentifierRepository));
			this.flows.Add (ProtocolFlowType.Subscribe, new SubscribeFlow (configuration, topicEvaluator, sessionRepository, packetIdentifierRepository, retainedRepository));
			this.flows.Add (ProtocolFlowType.Unsubscribe, new UnsubscribeFlow (sessionRepository, packetIdentifierRepository));
			this.flows.Add (ProtocolFlowType.Ping, new PingFlow ());
			this.flows.Add (ProtocolFlowType.Disconnect, new DisconnectFlow (clientManager, willRepository));
		}

		/// <exception cref="ProtocolException">ProtocolException</exception>
		public IProtocolFlow GetFlow (PacketType packetType)
		{
			var flow = default (IProtocolFlow);
			var flowType = packetType.ToFlowType ();

			if (!this.flows.TryGetValue (flowType, out flow)) {
				var error = string.Format (Resources.ProtocolFlowProvider_UnknownPacketType, packetType);
				
				throw new ProtocolException (error);
			}

			return flow;
		}
	}
}
