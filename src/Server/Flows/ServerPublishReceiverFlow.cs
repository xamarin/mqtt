using System.Linq;
using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class ServerPublishReceiverFlow : PublishReceiverFlow
	{
		readonly IPublishSenderFlow senderFlow;

		public ServerPublishReceiverFlow (IConnectionProvider connectionProvider, 
			ITopicEvaluator topicEvaluator,
			IRepository<RetainedMessage> retainedRepository, 
			IRepository<ClientSession> sessionRepository,
			IRepository<PacketIdentifier> packetIdentifierRepository,
			IPublishSenderFlow senderFlow,
			ProtocolConfiguration configuration)
			: base(connectionProvider, topicEvaluator, retainedRepository, sessionRepository, packetIdentifierRepository, configuration)
		{
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

			await this.DispatchToSubscribedClientsAsync (publish);
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
			var requestedQos = configuration.GetSupportedQos(subscription.MaximumQualityOfService);
			var packetId = this.packetIdentifierRepository.GetPacketIdentifier (requestedQos);
			var subscriptionPublish = new Publish (receivedPublish.Topic, requestedQos, retain: false, duplicated: false, packetId: packetId) {
				Payload = receivedPublish.Payload
			};

			await this.senderFlow.SendPublishAsync (subscription.ClientId, subscriptionPublish);
		}
	}
}
