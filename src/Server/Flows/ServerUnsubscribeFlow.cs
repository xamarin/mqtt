using System.Linq;
using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class ServerUnsubscribeFlow : IProtocolFlow
	{
		readonly IRepository<ClientSession> sessionRepository;

		public ServerUnsubscribeFlow (IRepository<ClientSession> sessionRepository)
		{
			this.sessionRepository = sessionRepository;
		}

		public async Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel)
		{
			if (input.Type != PacketType.Unsubscribe) {
				return;
			}

			var unsubscribe = input as Unsubscribe;
			var session = this.sessionRepository.Get (s => s.ClientId == clientId);

			if (session == null) {
				throw new ProtocolException (string.Format(Resources.SessionRepository_ClientSessionNotFound, clientId));
			}

			foreach (var topic in unsubscribe.Topics) {
				var subscription = session.Subscriptions.FirstOrDefault (s => s.TopicFilter == topic);

				if (subscription != null) {
					session.Subscriptions.Remove (subscription);
				}
			}

			this.sessionRepository.Update(session);

			await channel.SendAsync(new UnsubscribeAck (unsubscribe.PacketId));
		}
	}
}
