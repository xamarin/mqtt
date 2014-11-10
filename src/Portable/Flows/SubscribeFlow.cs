using System.Collections.Generic;
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
		readonly IRepository<ClientSubscription> subscriptionRepository;

		public SubscribeFlow (IProtocolConfiguration configuration, IRepository<ClientSubscription> subscriptionRepository)
		{
			this.configuration = configuration;
			this.subscriptionRepository = subscriptionRepository;
		}

		public async Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel)
		{
			if(input.Type == PacketType.SubscribeAck)
				return;

			var subscribe = input as Subscribe;

			if (subscribe == null) {
				var error = string.Format (Resources.ProtocolFlow_InvalidPacketType, input.Type, "Subscribe");

				throw new ProtocolException(error);
			}

			var returnCodes = new List<SubscribeReturnCode> ();

			foreach (var subscription in subscribe.Subscriptions) {
				try {
					var existingSubscription = this.subscriptionRepository.Get (s => s.ClientId == clientId && s.TopicFilter == subscription.Topic);

					if (existingSubscription != null) {
						existingSubscription.RequestedQualityOfService = subscription.RequestedQualityOfService;

						this.subscriptionRepository.Update (existingSubscription);
					} else {
						this.subscriptionRepository.Create (new ClientSubscription { 
							ClientId = clientId,
							TopicFilter = subscription.Topic,
							RequestedQualityOfService = subscription.RequestedQualityOfService });
					}

					var supportedQos = subscription.RequestedQualityOfService > this.configuration.SupportedQualityOfService ?
						this.configuration.SupportedQualityOfService : subscription.RequestedQualityOfService;
					var returnCode = supportedQos.ToReturnCode ();

					returnCodes.Add (returnCode);
				} catch (RepositoryException) {
					//TODO: Add some logging here and in other sections (to be defined)
					returnCodes.Add (SubscribeReturnCode.Failure);
				}
			}

			await channel.SendAsync(new SubscribeAck (subscribe.PacketId, returnCodes.ToArray()));
		}
	}
}
