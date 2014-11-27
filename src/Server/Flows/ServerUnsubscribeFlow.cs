using System.Linq;
using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class ServerUnsubscribeFlow : IProtocolFlow
	{
		readonly IConnectionProvider connectionProvider;
		readonly IRepository<ClientSession> sessionRepository;
		readonly IRepository<PacketIdentifier> packetIdentifierRepository;

		public ServerUnsubscribeFlow (IConnectionProvider connectionProvider, IRepository<ClientSession> sessionRepository, 
			IRepository<PacketIdentifier> packetIdentifierRepository)
		{
			this.connectionProvider = connectionProvider;
			this.sessionRepository = sessionRepository;
			this.packetIdentifierRepository = packetIdentifierRepository;
		}

		public async Task ExecuteAsync (string clientId, IPacket input)
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

			var channel = this.connectionProvider.GetConnection (clientId);

			await channel.SendAsync(new UnsubscribeAck (unsubscribe.PacketId));
		}
	}
}
