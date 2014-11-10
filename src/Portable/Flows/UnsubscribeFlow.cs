using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class UnsubscribeFlow : IProtocolFlow
	{
		readonly IRepository<ClientSubscription> subscriptionRepository;

		public UnsubscribeFlow (IRepository<ClientSubscription> subscriptionRepository)
		{
			this.subscriptionRepository = subscriptionRepository;
		}

		public async Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel)
		{
			if(input.Type == PacketType.UnsubscribeAck)
				return;

			var unsubscribe = input as Unsubscribe;

			if (unsubscribe == null) {
				var error = string.Format (Resources.ProtocolFlow_InvalidPacketType, input.Type, "Unsubscribe");

				throw new ProtocolException(error);
			}

			foreach (var topic in unsubscribe.Topics) {
				//TODO: Add exception handling to prevent any repository error
				var existingSubscription = this.subscriptionRepository.Get (s => s.ClientId == clientId && s.TopicFilter == topic);

				if (existingSubscription != null) {
					this.subscriptionRepository.Delete (existingSubscription);
				}
			}

			//TODO: Check this requirements of the spec:
			//If a Server deletes a Subscription:
			//It MUST stop adding any new messages for delivery to the Client [MQTT-3.10.4-2].
			//It MUST complete the delivery of any QoS 1 or QoS 2 messages which it has started to send to the Client [MQTT-3.10.4-3].
			//It MAY continue to deliver any existing messages buffered for delivery to the Client.

			await channel.SendAsync(new UnsubscribeAck (unsubscribe.PacketId));
		}
	}
}
