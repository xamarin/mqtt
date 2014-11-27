using System.Linq;
using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class PublishReceiverFlow : PublishFlow
	{
		readonly ITopicEvaluator topicEvaluator;
		readonly IRepository<RetainedMessage> retainedRepository;
		readonly IRepository<PacketIdentifier> packetIdentifierRepository;

		public PublishReceiverFlow (ProtocolConfiguration configuration, IClientManager clientManager, ITopicEvaluator topicEvaluator,
			IRepository<RetainedMessage> retainedRepository, 
			IRepository<ClientSession> sessionRepository,
			IRepository<PacketIdentifier> packetIdentifierRepository)
			: base(clientManager, sessionRepository, configuration)
		{
			this.topicEvaluator = topicEvaluator;
			this.retainedRepository = retainedRepository;
			this.packetIdentifierRepository = packetIdentifierRepository;
		}

		public override async Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel)
		{
			if (input.Type == PacketType.Publish) {
				var publish = input as Publish;

				await this.HandlePublishAsync (clientId, publish, channel);
			} else if (input.Type == PacketType.PublishRelease) {
				var publishRelease = input as PublishRelease;

				await this.HandlePublishReleaseAsync (clientId, publishRelease, channel);
			}
		}

		private async Task HandlePublishReleaseAsync(string clientId, PublishRelease publishRelease, IChannel<IPacket> channel)
		{
			var session = this.sessionRepository.Get (s => s.ClientId == clientId);
			var pendingAcknowledgement = session.PendingAcknowledgements
				.FirstOrDefault(u => u.Type == PacketType.PublishReceived &&
					u.PacketId == publishRelease.PacketId);

			session.PendingAcknowledgements.Remove (pendingAcknowledgement);

			this.sessionRepository.Update (session);

			await this.SendAckAsync (clientId, new PublishComplete (publishRelease.PacketId), channel);
		}

		private async Task HandlePublishAsync(string clientId, Publish publish, IChannel<IPacket> channel)
		{
			if (publish.QualityOfService != QualityOfService.AtMostOnce && !publish.PacketId.HasValue)
				throw new ProtocolException (Resources.PublishReceiverFlow_PacketIdRequired);

			if (publish.QualityOfService == QualityOfService.AtMostOnce && publish.PacketId.HasValue)
				throw new ProtocolException (Resources.PublishReceiverFlow_PacketIdNotAllowed);

			if (publish.Retain) {
				var existingRetainedMessage = this.retainedRepository.Get(r => r.Topic == publish.Topic);

				if(existingRetainedMessage != null) {
					this.retainedRepository.Delete(existingRetainedMessage);
				}

				if (publish.Payload.Length > 0) {
					var retainedMessage = new RetainedMessage {
						Topic = publish.Topic,
						QualityOfService = publish.QualityOfService,
						Payload = publish.Payload
					};

					this.retainedRepository.Create(retainedMessage);
				}
			}

			await this.DispatchToSubscribedClientsAsync (publish);

			var qos = this.GetMaximumQoS(publish.QualityOfService);

			if (qos == QualityOfService.AtMostOnce) {
				return;
			} else if (qos == QualityOfService.AtLeastOnce) {
				await this.SendAckAsync (clientId, new PublishAck (publish.PacketId.Value), channel);
			} else {

				await this.SendAckAsync (clientId, new PublishReceived (publish.PacketId.Value), channel);
			}
		}

		private async Task DispatchToSubscribedClientsAsync (Publish receivedPublish)
		{
			var sessions = this.sessionRepository.GetAll ();
			var subscriptions = sessions.SelectMany(s => s.Subscriptions)
				.Where(x => this.topicEvaluator.Matches(receivedPublish.Topic, x.TopicFilter));

			foreach (var subscription in subscriptions) {
				await this.DispatchToSubscribedClientAsync (subscription, receivedPublish);
			}
		}

		private async Task DispatchToSubscribedClientAsync (ClientSubscription subscription, Publish receivedPublish)
		{
			var requestedQos = this.GetMaximumQoS (subscription.MaximumQualityOfService);
			var packetId = this.packetIdentifierRepository.GetPacketIdentifier (requestedQos);
			var subscriptionPublish = new Publish (receivedPublish.Topic, requestedQos, retain: false, duplicated: false, packetId: packetId) {
				Payload = receivedPublish.Payload
			};
			var subscribedChannel = this.clientManager.GetConnection (subscription.ClientId);

			await this.SendPublishAsync (subscription.ClientId, subscriptionPublish, subscribedChannel);
		}

		private QualityOfService GetMaximumQoS(QualityOfService requestedQos)
		{
			return requestedQos > this.configuration.MaximumQualityOfService ? 
				this.configuration.MaximumQualityOfService : 
				requestedQos;
		}
	}
}
