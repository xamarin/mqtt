using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hermes.Exceptions;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class SubscribeFlow : IProtocolFlow
	{
		readonly IProtocolConfiguration configuration;
		readonly ITopicEvaluator topicEvaluator;
		readonly IRepository<ClientSession> sessionRepository;
		readonly IRepository<PacketIdentifier> packetIdentifierRepository;
		readonly IRepository<RetainedMessage> retainedRepository;

		public SubscribeFlow (IProtocolConfiguration configuration, ITopicEvaluator topicEvaluator, IRepository<ClientSession> sessionRepository, 
			IRepository<PacketIdentifier> packetIdentifierRepository, IRepository<RetainedMessage> retainedRepository)
		{
			this.configuration = configuration;
			this.topicEvaluator = topicEvaluator;
			this.sessionRepository = sessionRepository;
			this.packetIdentifierRepository = packetIdentifierRepository;
			this.retainedRepository = retainedRepository;
		}

		public async Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel)
		{
			if (input.Type == PacketType.SubscribeAck) {
				var subscribeAck = input as SubscribeAck;

				this.packetIdentifierRepository.Delete (i => i.Value == subscribeAck.PacketId);

				return;
			}

			var subscribe = input as Subscribe;

			if (subscribe == null) {
				var error = string.Format (Resources.ProtocolFlow_InvalidPacketType, input.Type, "Subscribe");

				throw new ProtocolException(error);
			}

			var session = this.sessionRepository.Get (s => s.ClientId == clientId);
			var returnCodes = new List<SubscribeReturnCode> ();

			foreach (var subscription in subscribe.Subscriptions) {
				try {
					var clientSubscription = session.Subscriptions.FirstOrDefault(s => s.TopicFilter == subscription.TopicFilter);

					if (clientSubscription != null) {
						clientSubscription.MaximumQualityOfService = subscription.MaximumQualityOfService;
					} else {
						clientSubscription = new ClientSubscription { 
							TopicFilter = subscription.TopicFilter,
							MaximumQualityOfService = subscription.MaximumQualityOfService 
						};

						session.Subscriptions.Add (clientSubscription);
					}

					var retainedMessages = this.retainedRepository.GetAll ().Where(r => this.topicEvaluator.Matches(r.Topic, clientSubscription.TopicFilter));

					if (retainedMessages != null) {
						foreach (var retainedMessage in retainedMessages) {
							var packetId = this.packetIdentifierRepository.GetPacketIdentifier (retainedMessage.QualityOfService);
							var publish = new Publish (retainedMessage.Topic, retainedMessage.QualityOfService, retain: true, duplicatedDelivery: false, packetId: packetId) {
								Payload = retainedMessage.Payload
							};

							await channel.SendAsync (publish);
						}
					}

					var supportedQos = subscription.MaximumQualityOfService > this.configuration.SupportedQualityOfService ?
						this.configuration.SupportedQualityOfService : subscription.MaximumQualityOfService;
					var returnCode = supportedQos.ToReturnCode ();

					returnCodes.Add (returnCode);
				} catch (RepositoryException) {
					//TODO: Add some logging here and in other sections (to be defined)
					returnCodes.Add (SubscribeReturnCode.Failure);
				}
			}

			this.sessionRepository.Update (session);

			await channel.SendAsync(new SubscribeAck (subscribe.PacketId, returnCodes.ToArray()));
		}
	}
}
