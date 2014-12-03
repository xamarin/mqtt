using System.Linq;
using System.Threading.Tasks;
using Hermes.Flows;
using Hermes.Packets;
using Hermes.Storage;

namespace Hermes
{
	public class PublishDispatcher : IPublishDispatcher
	{
		readonly IConnectionProvider connectionProvider;
		readonly ITopicEvaluator topicEvaluator;
		readonly IRepository<ClientSession> sessionRepository;
		readonly IRepository<PacketIdentifier> packetIdentifierRepository;
		readonly IPublishSenderFlow senderFlow;
		readonly ProtocolConfiguration configuration;

		public PublishDispatcher (IConnectionProvider connectionProvider, 
			ITopicEvaluator topicEvaluator,
			IRepositoryProvider repositoryProvider,
			IPublishSenderFlow senderFlow,
			ProtocolConfiguration configuration)
		{
			this.connectionProvider = connectionProvider;
			this.topicEvaluator = topicEvaluator;
			this.sessionRepository = repositoryProvider.GetRepository<ClientSession>();
			this.packetIdentifierRepository = repositoryProvider.GetRepository<PacketIdentifier>();
			this.senderFlow = senderFlow;
			this.configuration = configuration;
		}

		public async Task DispatchAsync (Publish publish)
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
