using System.Linq;
using System.Threading.Tasks;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Storage;

namespace System.Net.Mqtt.Flows
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
				throw new ProtocolException (string.Format(Properties.Resources.SessionRepository_ClientSessionNotFound, clientId));
			}

			foreach (var topic in unsubscribe.Topics) {
				var subscription = session.GetSubscriptions().FirstOrDefault (s => s.TopicFilter == topic);

				if (subscription != null) {
					session.RemoveSubscription (subscription);
				}
			}

			this.sessionRepository.Update(session);

			await channel.SendAsync(new UnsubscribeAck (unsubscribe.PacketId))
				.ConfigureAwait(continueOnCapturedContext: false);
		}
	}
}
