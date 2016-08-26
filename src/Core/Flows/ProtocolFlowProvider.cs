﻿using System.Collections.Generic;
using System.Linq;
using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Exceptions;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Storage;

namespace System.Net.Mqtt.Flows
{
	internal abstract class ProtocolFlowProvider : IProtocolFlowProvider
	{
		protected readonly IMqttTopicEvaluator topicEvaluator;
		protected readonly IRepositoryProvider repositoryProvider;
		protected readonly ITracerManager tracerManager;
		protected readonly MqttConfiguration configuration;

		IDictionary<ProtocolFlowType, IProtocolFlow> flows;

		protected ProtocolFlowProvider (IMqttTopicEvaluator topicEvaluator,
			IRepositoryProvider repositoryProvider,
			ITracerManager tracerManager,
			MqttConfiguration configuration)
		{
			this.topicEvaluator = topicEvaluator;
			this.repositoryProvider = repositoryProvider;
			this.tracerManager = tracerManager;
			this.configuration = configuration;
		}

		protected abstract IDictionary<ProtocolFlowType, IProtocolFlow> InitializeFlows ();

		protected abstract bool IsValidPacketType (MqttPacketType packetType);

		/// <exception cref="MqttException">ProtocolException</exception>
		public IProtocolFlow GetFlow (MqttPacketType packetType)
		{
			if (!IsValidPacketType (packetType)) {
				var error = string.Format (Resources.ProtocolFlowProvider_InvalidPacketType, packetType);

				throw new MqttException (error);
			}

			var flow = default (IProtocolFlow);
			var flowType = packetType.ToFlowType();

			if (!GetFlows ().TryGetValue (flowType, out flow)) {
				var error = string.Format (Resources.ProtocolFlowProvider_UnknownPacketType, packetType);

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
