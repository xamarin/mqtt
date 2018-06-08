using System.Collections.Generic;
using System.Linq;
using System.Net.Mqtt.Sdk.Packets;
using System.Net.Mqtt.Sdk.Storage;

namespace System.Net.Mqtt.Sdk.Flows
{
	internal abstract class ProtocolFlowProvider : IProtocolFlowProvider
	{
		protected readonly IMqttTopicEvaluator topicEvaluator;
		protected readonly IRepositoryProvider repositoryProvider;
		protected readonly MqttConfiguration configuration;

		readonly object lockObject = new object ();
		IDictionary<ProtocolFlowType, IProtocolFlow> flows;

		protected ProtocolFlowProvider (IMqttTopicEvaluator topicEvaluator,
			IRepositoryProvider repositoryProvider,
			MqttConfiguration configuration)
		{
			this.topicEvaluator = topicEvaluator;
			this.repositoryProvider = repositoryProvider;
			this.configuration = configuration;
		}

		protected abstract IDictionary<ProtocolFlowType, IProtocolFlow> InitializeFlows ();

		protected abstract bool IsValidPacketType (MqttPacketType packetType);

		public IProtocolFlow GetFlow (MqttPacketType packetType)
		{
			if (!IsValidPacketType (packetType)) {
				var error = string.Format (Properties.Resources.ProtocolFlowProvider_InvalidPacketType, packetType);

				throw new MqttException (error);
			}

			var flow = default (IProtocolFlow);
			var flowType = packetType.ToFlowType();

			if (!GetFlows ().TryGetValue (flowType, out flow)) {
				var error = string.Format (Properties.Resources.ProtocolFlowProvider_UnknownPacketType, packetType);

				throw new MqttException (error);
			}

			return flow;
		}

		public T GetFlow<T> ()
			where T : class, IProtocolFlow
		{
			var pair = GetFlows().FirstOrDefault (f => f.Value is T);

			if (pair.Equals (default (KeyValuePair<ProtocolFlowType, IProtocolFlow>))) {
				return default (T);
			}

			return pair.Value as T;
		}

		IDictionary<ProtocolFlowType, IProtocolFlow> GetFlows ()
		{
			if (flows == null) {
				lock (lockObject) {
					if (flows == null){
						flows = InitializeFlows ();
					}
				}
			}

			return flows;
		}
	}
}
