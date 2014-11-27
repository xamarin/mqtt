using System.Linq;
using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class ServerUnsubscribeFlow : IProtocolFlow
	{
		readonly IRepository<ClientSession> sessionRepository;
		readonly IRepository<PacketIdentifier> packetIdentifierRepository;

		public ServerUnsubscribeFlow (IRepository<ClientSession> sessionRepository, IRepository<PacketIdentifier> packetIdentifierRepository)
		{
			this.sessionRepository = sessionRepository;
			this.packetIdentifierRepository = packetIdentifierRepository;
		}

		public async Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel)
		{
			if (input.Type != PacketType.Unsubscribe)
				return;

			var unsubscribe = input as Unsubscribe;
			var session = this.sessionRepository.Get (s => s.ClientId == clientId);

			foreach (var topic in unsubscribe.Topics) {
				var subscription = session.Subscriptions.FirstOrDefault (s => s.TopicFilter == topic);

				if (subscription != null) {
					session.Subscriptions.Remove (subscription);
				}
			}

			this.sessionRepository.Update(session);

			//TODO: Check this requirements of the spec:
			//If a Server deletes a Subscription:
			//It MUST stop adding any new messages for delivery to the Client [MQTT-3.10.4-2].
			//It MUST complete the delivery of any QoS 1 or QoS 2 messages which it has started to send to the Client [MQTT-3.10.4-3].
			//It MAY continue to deliver any existing messages buffered for delivery to the Client.

			await channel.SendAsync(new UnsubscribeAck (unsubscribe.PacketId));
		}
	}
}
