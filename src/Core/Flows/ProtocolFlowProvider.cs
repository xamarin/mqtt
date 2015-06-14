using System.Collections.Generic;
using System.Linq;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Storage;

namespace System.Net.Mqtt.Flows
{
	public abstract class ProtocolFlowProvider : IProtocolFlowProvider
	{
		protected readonly ITopicEvaluator topicEvaluator;
		protected readonly IRepositoryProvider repositoryProvider;
		protected readonly ProtocolConfiguration configuration;

		IDictionary<ProtocolFlowType, IProtocolFlow> flows;

		protected ProtocolFlowProvider (ITopicEvaluator topicEvaluator, 
			IRepositoryProvider repositoryProvider, 
			ProtocolConfiguration configuration)
		{
			this.topicEvaluator = topicEvaluator;
			this.repositoryProvider = repositoryProvider;
			this.configuration = configuration;
		}

		protected abstract IDictionary<ProtocolFlowType, IProtocolFlow> InitializeFlows ();

		protected abstract bool IsValidPacketType (PacketType packetType);

		/// <exception cref="ProtocolException">ProtocolException</exception>
		public IProtocolFlow GetFlow (PacketType packetType)
		{
			if (!this.IsValidPacketType (packetType)) {
				var error = string.Format (Properties.Resources.ProtocolFlowProvider_InvalidPacketType, packetType);
				
				throw new ProtocolException (error);
			}

			var flow = default (IProtocolFlow);
			var flowType = packetType.ToFlowType();

			if (!this.GetFlows().TryGetValue (flowType, out flow)) {
				var error = string.Format (Properties.Resources.ProtocolFlowProvider_UnknownPacketType, packetType);
				
				throw new ProtocolException (error);
			}

			return flow;
		}

		public T GetFlow<T> () 
			where T : class, IProtocolFlow
		{
			var pair = this.GetFlows().FirstOrDefault (f => f.Value is T);

			if (pair.Equals (default (KeyValuePair<ProtocolFlowType, IProtocolFlow>))) {
				return default (T);
			}

			return pair.Value as T;
		}

		private IDictionary<ProtocolFlowType, IProtocolFlow> GetFlows()
		{
			if (this.flows == default (IDictionary<ProtocolFlowType, IProtocolFlow>)) {
				this.flows = this.InitializeFlows ();
			}

			return this.flows;
		}
	}
}
