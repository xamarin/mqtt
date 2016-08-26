using System.Collections.Generic;
using System.Linq;
using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Exceptions;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Storage;

namespace System.Net.Mqtt.Flows
{
	internal abstract class ProtocolFlowProvider : IProtocolFlowProvider
	{
		protected readonly ITopicEvaluator topicEvaluator;
		protected readonly IRepositoryProvider repositoryProvider;
		protected readonly ITracerManager tracerManager;
		protected readonly ProtocolConfiguration configuration;

		IDictionary<ProtocolFlowType, IProtocolFlow> flows;

		protected ProtocolFlowProvider (ITopicEvaluator topicEvaluator,
			IRepositoryProvider repositoryProvider,
			ITracerManager tracerManager,
			ProtocolConfiguration configuration)
		{
			this.topicEvaluator = topicEvaluator;
			this.repositoryProvider = repositoryProvider;
			this.tracerManager = tracerManager;
			this.configuration = configuration;
		}

		protected abstract IDictionary<ProtocolFlowType, IProtocolFlow> InitializeFlows ();

		protected abstract bool IsValidPacketType (PacketType packetType);

		/// <exception cref="MqttException">ProtocolException</exception>
		public IProtocolFlow GetFlow (PacketType packetType)
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
			if (flows == default (IDictionary<ProtocolFlowType, IProtocolFlow>)) {
				flows = InitializeFlows ();
			}

			return flows;
		}
	}
}
