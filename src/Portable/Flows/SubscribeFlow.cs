using System;
using System.Collections.Generic;
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
		readonly IRepository<ConnectionRefused> connectionRefusedRepository;

		public SubscribeFlow (IProtocolConfiguration configuration, IRepository<ClientSubscription> subscriptionRepository, IRepository<ConnectionRefused> connectionRefusedRepository)
		{
			this.configuration = configuration;
			this.subscriptionRepository = subscriptionRepository;
			this.connectionRefusedRepository = connectionRefusedRepository;
		}

		public IPacket Apply (IPacket input, IProtocolConnection connection)
		{
			if (input.Type != PacketType.Subscribe && input.Type != PacketType.SubscribeAck) {
				var error = string.Format (Resources.ProtocolFlow_InvalidPacketType, input.Type, "Subscribe");

				throw new ProtocolException(error);
			}

			if (this.connectionRefusedRepository.Exist (c => c.ConnectionId == connection.Id)) {
				var error = string.Format (Resources.ProtocolFlow_ConnectionRejected, connection.Id);

				throw new ProtocolException(error);
			}

			if (connection.IsPending)
				throw new ProtocolException (Resources.ProtocolFlow_ConnectRequired);

			if(input.Type == PacketType.SubscribeAck)
				return default(IPacket);

			var subscribe = input as Subscribe;
			var returnCodes = new List<SubscribeReturnCode> ();

			foreach (var subscription in subscribe.Subscriptions) {
				try {
					var existingSubscription = this.subscriptionRepository.Get (s => s.ClientId == connection.ClientId && s.TopicFilter == subscription.Topic);

					if (existingSubscription != null) {
						existingSubscription.RequestedQualityOfService = subscription.RequestedQualityOfService;

						this.subscriptionRepository.Update (existingSubscription);
					} else {
						this.subscriptionRepository.Create (new ClientSubscription { 
							ClientId = connection.ClientId,
							TopicFilter = subscription.Topic,
							RequestedQualityOfService = subscription.RequestedQualityOfService });
					}

					var supportedQos = subscription.RequestedQualityOfService > this.configuration.SupportedQualityOfService ?
						this.configuration.SupportedQualityOfService : subscription.RequestedQualityOfService;
					var returnCode = SubscribeReturnCode.Failure;

					Enum.TryParse (supportedQos.ToString(), out returnCode);

					returnCodes.Add (returnCode);
				} catch (RepositoryException) {
					//TODO: Add some logging here and in other sections (to be defined)
					returnCodes.Add (SubscribeReturnCode.Failure);
				}
			}

			return new SubscribeAck (subscribe.PacketId, returnCodes.ToArray());
		}
	}
}
