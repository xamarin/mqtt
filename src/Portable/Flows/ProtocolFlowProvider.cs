using System.Collections.Generic;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes.Flows
{
	public abstract class ProtocolFlowProvider : IProtocolFlowProvider
	{
		protected readonly IDictionary<ProtocolFlowType, IProtocolFlow> flows;
		protected readonly IClientManager clientManager;
		protected readonly ITopicEvaluator topicEvaluator;
		protected readonly IRepositoryFactory repositoryFactory;
		protected readonly ProtocolConfiguration configuration;

		protected ProtocolFlowProvider (IClientManager clientManager, ITopicEvaluator topicEvaluator,
			IRepositoryFactory repositoryFactory, ProtocolConfiguration configuration)
		{
			this.clientManager = clientManager;
			this.topicEvaluator = topicEvaluator;
			this.repositoryFactory = repositoryFactory;
			this.configuration = configuration;

			this.flows = this.GetFlows ();
		}

		protected abstract IDictionary<ProtocolFlowType, IProtocolFlow> GetFlows ();

		protected abstract bool IsValidPacketType (PacketType packetType);

		/// <exception cref="ProtocolException">ProtocolException</exception>
		public IProtocolFlow GetFlow (PacketType packetType)
		{
			if (!this.IsValidPacketType (packetType)) {
				var error = string.Format (Resources.ProtocolFlowProvider_InvalidPacketType, packetType);
				
				throw new ProtocolException (error);
			}

			var flow = default (IProtocolFlow);
			var flowType = packetType.ToFlowType();

			if (!this.flows.TryGetValue (flowType, out flow)) {
				var error = string.Format (Resources.ProtocolFlowProvider_UnknownPacketType, packetType);
				
				throw new ProtocolException (error);
			}

			return flow;
		}
	}
}
