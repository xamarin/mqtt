using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class UnsubscribeFlow : IProtocolFlow
	{
		readonly IRepository<ClientSubscription> subscriptionRepository;
		readonly IRepository<ConnectionRefused> connectionRefusedRepository;

		public UnsubscribeFlow (IRepository<ClientSubscription> subscriptionRepository, IRepository<ConnectionRefused> connectionRefusedRepository)
		{
			this.subscriptionRepository = subscriptionRepository;
			this.connectionRefusedRepository = connectionRefusedRepository;
		}

		public IPacket Apply (IPacket input, IProtocolConnection connection)
		{
			if (input.Type != PacketType.Unsubscribe && input.Type != PacketType.UnsubscribeAck) {
				var error = string.Format (Resources.ProtocolFlow_InvalidPacketType, input.Type, "Unsubscribe");

				throw new ProtocolException(error);
			}

			//TODO: Find a way of encapsulating this logic to avoid repeating it in each flow
			if (this.connectionRefusedRepository.Exist (c => c.ConnectionId == connection.Id)) {
				var error = string.Format (Resources.ProtocolFlow_ConnectionRejected, connection.Id);

				throw new ProtocolException(error);
			}

			if (connection.IsPending)
				throw new ProtocolException (Resources.ProtocolFlow_ConnectRequired);

			if(input.Type == PacketType.UnsubscribeAck)
				return default(IPacket);

			var unsubscribe = input as Unsubscribe;

			foreach (var topic in unsubscribe.Topics) {
				//TODO: Add exception handling to prevent any repository error
				var existingSubscription = this.subscriptionRepository.Get (s => s.ClientId == connection.ClientId && s.TopicFilter == topic);

				if (existingSubscription != null) {
					this.subscriptionRepository.Delete (existingSubscription);
				}
			}

			//TODO: Check this requirements of the spec:
			//If a Server deletes a Subscription:
			//It MUST stop adding any new messages for delivery to the Client [MQTT-3.10.4-2].
			//It MUST complete the delivery of any QoS 1 or QoS 2 messages which it has started to send to the Client [MQTT-3.10.4-3].
			//It MAY continue to deliver any existing messages buffered for delivery to the Client.

			return new UnsubscribeAck (unsubscribe.PacketId);
		}
	}
}
