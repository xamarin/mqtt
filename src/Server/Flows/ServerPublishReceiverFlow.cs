using System.Linq;
using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class ServerPublishReceiverFlow : PublishReceiverFlow
	{
		readonly IConnectionProvider connectionProvider;
		readonly IPublishSenderFlow senderFlow;

		public ServerPublishReceiverFlow (ITopicEvaluator topicEvaluator,
			IConnectionProvider connectionProvider,
			IPublishSenderFlow senderFlow,
			IRepository<RetainedMessage> retainedRepository, 
			IRepository<ClientSession> sessionRepository,
			IRepository<PacketIdentifier> packetIdentifierRepository,
			ProtocolConfiguration configuration)
			: base(topicEvaluator, retainedRepository, sessionRepository, packetIdentifierRepository, configuration)
		{
			this.connectionProvider = connectionProvider;
			this.senderFlow = senderFlow;
		}

		protected override async Task ProcessPublishAsync (Publish publish)
		{
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

			await this.DispatchAsync (publish);
		}

		private async Task DispatchAsync (Publish publish)
		{
			var sessions = this.sessionRepository.GetAll ();
			var subscriptions = sessions.SelectMany(s => s.Subscriptions)
				.Where(x => this.topicEvaluator.Matches(publish.Topic, x.TopicFilter));

			foreach (var subscription in subscriptions) {
				await this.DispatchAsync (subscription, publish);
			}
		}

		private async Task DispatchAsync (ClientSubscription subscription, Publish publish)
		{
			var requestedQos = configuration.GetSupportedQos(subscription.MaximumQualityOfService);
			var packetId = this.packetIdentifierRepository.GetPacketIdentifier (requestedQos);
			var subscriptionPublish = new Publish (publish.Topic, requestedQos, retain: false, duplicated: false, packetId: packetId) {
				Payload = publish.Payload
			};
			var clientChannel = this.connectionProvider.GetConnection (subscription.ClientId);

			await this.senderFlow.SendPublishAsync (subscription.ClientId, subscriptionPublish, clientChannel);
		}
	}
}
