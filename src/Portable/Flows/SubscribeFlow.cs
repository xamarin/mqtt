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
		readonly IRepository<ClientSession> sessionRepository;
		readonly IRepository<PacketIdentifier> packetIdentifierRepository;

		public SubscribeFlow (IProtocolConfiguration configuration, IRepository<ClientSession> sessionRepository, 
			IRepository<PacketIdentifier> packetIdentifierRepository)
		{
			this.configuration = configuration;
			this.sessionRepository = sessionRepository;
			this.packetIdentifierRepository = packetIdentifierRepository;
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
					var existingSubscription = session.Subscriptions.FirstOrDefault(s => s.TopicFilter == subscription.TopicFilter);

					if (existingSubscription != null) {
						existingSubscription.MaximumQualityOfService = subscription.MaximumQualityOfService;
					} else {
						session.Subscriptions.Add (new ClientSubscription { 
							TopicFilter = subscription.TopicFilter,
							MaximumQualityOfService = subscription.MaximumQualityOfService });
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
