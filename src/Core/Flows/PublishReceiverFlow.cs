﻿using System.Linq;
using System.Threading.Tasks;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Storage;
using System.Net.Mqtt.Exceptions;
using System.Net.Mqtt.Diagnostics;

namespace System.Net.Mqtt.Flows
{
	internal class PublishReceiverFlow : PublishFlow
	{
		protected readonly IMqttTopicEvaluator topicEvaluator;
		protected readonly IRepository<RetainedMessage> retainedRepository;

		public PublishReceiverFlow (IMqttTopicEvaluator topicEvaluator,
			IRepository<RetainedMessage> retainedRepository,
			IRepository<ClientSession> sessionRepository,
			ITracerManager tracerManager,
			MqttConfiguration configuration)
			: base (sessionRepository, tracerManager, configuration)
		{
			this.topicEvaluator = topicEvaluator;
			this.retainedRepository = retainedRepository;
		}

		public override async Task ExecuteAsync (string clientId, IPacket input, IMqttChannel<IPacket> channel)
		{
			if (input.Type == MqttPacketType.Publish) {
				var publish = input as Publish;

				await HandlePublishAsync (clientId, publish, channel)
					.ConfigureAwait (continueOnCapturedContext: false);
			} else if (input.Type == MqttPacketType.PublishRelease) {
				var publishRelease = input as PublishRelease;

				await HandlePublishReleaseAsync (clientId, publishRelease, channel)
					.ConfigureAwait (continueOnCapturedContext: false);
			}
		}

		protected virtual Task ProcessPublishAsync (Publish publish, string clientId)
		{
			return Task.Delay (0);
		}

		async Task HandlePublishAsync (string clientId, Publish publish, IMqttChannel<IPacket> channel)
		{
			if (publish.QualityOfService != MqttQualityOfService.AtMostOnce && !publish.PacketId.HasValue) {
				throw new MqttException (Resources.PublishReceiverFlow_PacketIdRequired);
			}

			if (publish.QualityOfService == MqttQualityOfService.AtMostOnce && publish.PacketId.HasValue) {
				throw new MqttException (Resources.PublishReceiverFlow_PacketIdNotAllowed);
			}

			var qos = configuration.GetSupportedQos(publish.QualityOfService);
			var session = sessionRepository.Get (s => s.ClientId == clientId);

			if (session == null) {
				throw new MqttException (string.Format (Resources.SessionRepository_ClientSessionNotFound, clientId));
			}

			if (qos == MqttQualityOfService.ExactlyOnce && session.GetPendingAcknowledgements ().Any (ack => ack.Type == MqttPacketType.PublishReceived && ack.PacketId == publish.PacketId.Value)) {
				await SendQosAck (clientId, qos, publish, channel)
					.ConfigureAwait (continueOnCapturedContext: false);

				return;
			}

			await SendQosAck (clientId, qos, publish, channel)
				.ConfigureAwait (continueOnCapturedContext: false);
			await ProcessPublishAsync (publish, clientId)
				.ConfigureAwait (continueOnCapturedContext: false);
		}

		async Task HandlePublishReleaseAsync (string clientId, PublishRelease publishRelease, IMqttChannel<IPacket> channel)
		{
			RemovePendingAcknowledgement (clientId, publishRelease.PacketId, MqttPacketType.PublishReceived);

			await SendAckAsync (clientId, new PublishComplete (publishRelease.PacketId), channel)
				.ConfigureAwait (continueOnCapturedContext: false);
		}

		async Task SendQosAck (string clientId, MqttQualityOfService qos, Publish publish, IMqttChannel<IPacket> channel)
		{
			if (qos == MqttQualityOfService.AtMostOnce) {
				return;
			} else if (qos == MqttQualityOfService.AtLeastOnce) {
				await SendAckAsync (clientId, new PublishAck (publish.PacketId.Value), channel)
					.ConfigureAwait (continueOnCapturedContext: false);
			} else {
				await SendAckAsync (clientId, new PublishReceived (publish.PacketId.Value), channel)
					.ConfigureAwait (continueOnCapturedContext: false);
			}
		}
	}
}
