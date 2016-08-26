using System.Linq;
using System.Threading.Tasks;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Storage;
using System.Net.Mqtt.Exceptions;
using System.Net.Mqtt.Diagnostics;

namespace System.Net.Mqtt.Flows
{
	internal class PublishReceiverFlow : PublishFlow
	{
		protected readonly ITopicEvaluator topicEvaluator;
		protected readonly IRepository<RetainedMessage> retainedRepository;

		public PublishReceiverFlow (ITopicEvaluator topicEvaluator,
			IRepository<RetainedMessage> retainedRepository,
			IRepository<ClientSession> sessionRepository,
			ITracerManager tracerManager,
			ProtocolConfiguration configuration)
			: base (sessionRepository, tracerManager, configuration)
		{
			this.topicEvaluator = topicEvaluator;
			this.retainedRepository = retainedRepository;
		}

		public override async Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel)
		{
			if (input.Type == PacketType.Publish) {
				var publish = input as Publish;

				await HandlePublishAsync (clientId, publish, channel)
					.ConfigureAwait (continueOnCapturedContext: false);
			} else if (input.Type == PacketType.PublishRelease) {
				var publishRelease = input as PublishRelease;

				await HandlePublishReleaseAsync (clientId, publishRelease, channel)
					.ConfigureAwait (continueOnCapturedContext: false);
			}
		}

		protected virtual Task ProcessPublishAsync (Publish publish, string clientId)
		{
			return Task.Delay (0);
		}

		async Task HandlePublishAsync (string clientId, Publish publish, IChannel<IPacket> channel)
		{
			if (publish.QualityOfService != QualityOfService.AtMostOnce && !publish.PacketId.HasValue) {
				throw new MqttException (Properties.Resources.PublishReceiverFlow_PacketIdRequired);
			}

			if (publish.QualityOfService == QualityOfService.AtMostOnce && publish.PacketId.HasValue) {
				throw new MqttException (Properties.Resources.PublishReceiverFlow_PacketIdNotAllowed);
			}

			var qos = configuration.GetSupportedQos(publish.QualityOfService);
			var session = sessionRepository.Get (s => s.ClientId == clientId);

			if (session == null) {
				throw new MqttException (string.Format (Properties.Resources.SessionRepository_ClientSessionNotFound, clientId));
			}

			if (qos == QualityOfService.ExactlyOnce && session.GetPendingAcknowledgements ().Any (ack => ack.Type == PacketType.PublishReceived && ack.PacketId == publish.PacketId.Value)) {
				await SendQosAck (clientId, qos, publish, channel)
					.ConfigureAwait (continueOnCapturedContext: false);

				return;
			}

			await SendQosAck (clientId, qos, publish, channel)
				.ConfigureAwait (continueOnCapturedContext: false);
			await ProcessPublishAsync (publish, clientId)
				.ConfigureAwait (continueOnCapturedContext: false);
		}

		async Task HandlePublishReleaseAsync (string clientId, PublishRelease publishRelease, IChannel<IPacket> channel)
		{
			RemovePendingAcknowledgement (clientId, publishRelease.PacketId, PacketType.PublishReceived);

			await SendAckAsync (clientId, new PublishComplete (publishRelease.PacketId), channel)
				.ConfigureAwait (continueOnCapturedContext: false);
		}

		async Task SendQosAck (string clientId, QualityOfService qos, Publish publish, IChannel<IPacket> channel)
		{
			if (qos == QualityOfService.AtMostOnce) {
				return;
			} else if (qos == QualityOfService.AtLeastOnce) {
				await SendAckAsync (clientId, new PublishAck (publish.PacketId.Value), channel)
					.ConfigureAwait (continueOnCapturedContext: false);
			} else {
				await SendAckAsync (clientId, new PublishReceived (publish.PacketId.Value), channel)
					.ConfigureAwait (continueOnCapturedContext: false);
			}
		}
	}
}
