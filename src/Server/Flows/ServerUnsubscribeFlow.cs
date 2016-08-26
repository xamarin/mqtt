using System.Linq;
using System.Threading.Tasks;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Storage;
using System.Net.Mqtt.Exceptions;

namespace System.Net.Mqtt.Flows
{
	internal class ServerUnsubscribeFlow : IProtocolFlow
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
			var session = sessionRepository.Get (s => s.ClientId == clientId);

			if (session == null) {
				throw new MqttException (string.Format (Properties.Resources.SessionRepository_ClientSessionNotFound, clientId));
			}

			foreach (var topic in unsubscribe.Topics) {
				var subscription = session.GetSubscriptions().FirstOrDefault (s => s.TopicFilter == topic);

				if (subscription != null) {
					session.RemoveSubscription (subscription);
				}
			}

			sessionRepository.Update (session);

			await channel.SendAsync (new UnsubscribeAck (unsubscribe.PacketId))
				.ConfigureAwait (continueOnCapturedContext: false);
		}
	}
}
