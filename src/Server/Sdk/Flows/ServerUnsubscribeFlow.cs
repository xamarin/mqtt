using System.Linq;
using System.Threading.Tasks;
using System.Net.Mqtt.Sdk.Packets;
using System.Net.Mqtt.Sdk.Storage;

namespace System.Net.Mqtt.Sdk.Flows
{
	internal class ServerUnsubscribeFlow : IProtocolFlow
	{
		readonly IRepository<ClientSession> sessionRepository;

		public ServerUnsubscribeFlow (IRepository<ClientSession> sessionRepository)
		{
			this.sessionRepository = sessionRepository;
		}

		public async Task ExecuteAsync (string clientId, IPacket input, IMqttChannel<IPacket> channel)
		{
			if (input.Type != MqttPacketType.Unsubscribe) {
				return;
			}

			var unsubscribe = input as Unsubscribe;
			var session = sessionRepository.Read (clientId);

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
