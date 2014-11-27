using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hermes.Exceptions;
using Hermes.Packets;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class ServerSubscribeFlow : IProtocolFlow
	{
		readonly ITopicEvaluator topicEvaluator;
		readonly IRepository<ClientSession> sessionRepository;
		readonly IRepository<PacketIdentifier> packetIdentifierRepository;
		readonly IRepository<RetainedMessage> retainedRepository;
		readonly IPublishFlow publishFlow;
		readonly ProtocolConfiguration configuration;

		public ServerSubscribeFlow (ITopicEvaluator topicEvaluator, IRepository<ClientSession> sessionRepository, 
			IRepository<PacketIdentifier> packetIdentifierRepository, 
			IRepository<RetainedMessage> retainedRepository,
			IPublishFlow publishFlow,
			ProtocolConfiguration configuration)
		{
			this.topicEvaluator = topicEvaluator;
			this.sessionRepository = sessionRepository;
			this.packetIdentifierRepository = packetIdentifierRepository;
			this.retainedRepository = retainedRepository;
			this.publishFlow = publishFlow;
			this.configuration = configuration;
		}

		public async Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel)
		{
			if (input.Type != PacketType.Subscribe)
				return;

			var subscribe = input as Subscribe;
			var session = this.sessionRepository.Get (s => s.ClientId == clientId);
			var returnCodes = new List<SubscribeReturnCode> ();

			foreach (var subscription in subscribe.Subscriptions) {
				try {
					if (!this.topicEvaluator.IsValidTopicFilter (subscription.TopicFilter)) {
						returnCodes.Add (SubscribeReturnCode.Failure);
						continue;
					}

					var clientSubscription = session.Subscriptions.FirstOrDefault(s => s.TopicFilter == subscription.TopicFilter);

					if (clientSubscription != null) {
						clientSubscription.MaximumQualityOfService = subscription.MaximumQualityOfService;
					} else {
						clientSubscription = new ClientSubscription { 
							ClientId = clientId,
							TopicFilter = subscription.TopicFilter,
							MaximumQualityOfService = subscription.MaximumQualityOfService 
						};

						session.Subscriptions.Add (clientSubscription);
					}

					await this.SendRetainedMessagesAsync (clientSubscription, channel);
		
					var supportedQos = subscription.MaximumQualityOfService > this.configuration.MaximumQualityOfService ?
						this.configuration.MaximumQualityOfService : subscription.MaximumQualityOfService;
					var returnCode = supportedQos.ToReturnCode ();

					returnCodes.Add (returnCode);
				} catch (RepositoryException) {
					returnCodes.Add (SubscribeReturnCode.Failure);
				}
			}

			this.sessionRepository.Update (session);

			await channel.SendAsync(new SubscribeAck (subscribe.PacketId, returnCodes.ToArray()));
		}

		private async Task SendRetainedMessagesAsync(ClientSubscription subscription, IChannel<IPacket> channel)
		{
			var retainedMessages = this.retainedRepository.GetAll ()
				.Where(r => this.topicEvaluator.Matches(r.Topic, subscription.TopicFilter));

			if (retainedMessages != null) {
				foreach (var retainedMessage in retainedMessages) {
					var packetId = this.packetIdentifierRepository.GetPacketIdentifier (subscription.MaximumQualityOfService);
					var publish = new Publish (retainedMessage.Topic, subscription.MaximumQualityOfService, 
						retain: true, duplicated: false, packetId: packetId) {
						Payload = retainedMessage.Payload
					};

					await this.publishFlow.SendPublishAsync(subscription.ClientId, publish, channel);
				}
			}
		}
	}
}
