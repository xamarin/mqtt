using System.Linq;
using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class PublishReceiverFlow : PublishFlow
	{
		protected readonly ITopicEvaluator topicEvaluator;
		protected readonly IRepository<RetainedMessage> retainedRepository;
		protected readonly IRepository<PacketIdentifier> packetIdentifierRepository;

		public PublishReceiverFlow (ITopicEvaluator topicEvaluator,
			IRepository<RetainedMessage> retainedRepository, 
			IRepository<ClientSession> sessionRepository,
			IRepository<PacketIdentifier> packetIdentifierRepository,
			ProtocolConfiguration configuration)
			: base(sessionRepository, configuration)
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

		protected virtual Task ProcessPublishAsync(Publish publish)
		{
			return Task.Delay (0);
		}

		private async Task HandlePublishAsync(string clientId, Publish publish, IChannel<IPacket> channel)
		{
			if (publish.QualityOfService != QualityOfService.AtMostOnce && !publish.PacketId.HasValue)
				throw new ProtocolException (Resources.PublishReceiverFlow_PacketIdRequired);

			if (publish.QualityOfService == QualityOfService.AtMostOnce && publish.PacketId.HasValue)
				throw new ProtocolException (Resources.PublishReceiverFlow_PacketIdNotAllowed);
			
			var qos = configuration.GetSupportedQos(publish.QualityOfService);
			var session = this.sessionRepository.Get (s => s.ClientId == clientId);

			if(qos == QualityOfService.ExactlyOnce && 
				session.PendingAcknowledgements.Any(ack => ack.Type == PacketType.PublishReceived &&
					ack.PacketId == publish.PacketId.Value)) {
				await this.SendQosAck (clientId, qos, publish, channel);

				return;
			}

			await this.ProcessPublishAsync(publish);
			await this.SendQosAck (clientId, qos, publish, channel);
		}

		private async Task HandlePublishReleaseAsync(string clientId, PublishRelease publishRelease, IChannel<IPacket> channel)
		{
			this.RemovePendingAcknowledgement (clientId, publishRelease.PacketId, PacketType.PublishReceived);

			await this.SendAckAsync (clientId, new PublishComplete (publishRelease.PacketId), channel);
		}

		private async Task SendQosAck(string clientId, QualityOfService qos, Publish publish, IChannel<IPacket> channel)
		{
			if (qos == QualityOfService.AtMostOnce) {
				return;
			} else if (qos == QualityOfService.AtLeastOnce) {
				await this.SendAckAsync (clientId, new PublishAck (publish.PacketId.Value), channel);
			} else {
				await this.SendAckAsync (clientId, new PublishReceived (publish.PacketId.Value), channel);
			}
		}
	}
}
