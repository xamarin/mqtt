using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes.Flows
{
	//TODO: Pending to add Session State entity and behavior
	//TODO: Pending to add support for this:  If the ClientId represents a Client already connected to the Server then the Server MUST disconnect the existing Client
	public class ConnectFlow : IProtocolFlow
	{
		readonly IRepository<ClientSession> sessionRepository;
		readonly IRepository<ConnectionWill> willRepository;

		public ConnectFlow (IRepository<ClientSession> sessionRepository, IRepository<ConnectionWill> willRepository)
		{
			this.sessionRepository = sessionRepository;
			this.willRepository = willRepository;
		}

		public async Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel)
		{
			if (input.Type == PacketType.ConnectAck)
				return;

			var connect = input as Connect;

			if (connect == null) {
				var error = string.Format (Resources.ProtocolFlow_InvalidPacketType, input.Type, "Connect");

				throw new ProtocolException(error);
			}

			//TODO: Add exception handling to prevent any repository error
			var session = this.sessionRepository.Get (s => s.ClientId == clientId);
			var sessionPresent = connect.CleanSession ? false : session != null;

			if (connect.CleanSession && session != null) {
				this.sessionRepository.Delete(session);
				session = null; //TODO: Verify if this is necessary
			}

			if (session == null) {
				session = new ClientSession { ClientId = clientId, Clean = connect.CleanSession };

				this.sessionRepository.Create (session);
			}

			if (connect.Will != null) {
				var connectionWill = new ConnectionWill { ClientId = clientId, Will = connect.Will };

				this.willRepository.Create (connectionWill);
			}

			await channel.SendAsync(new ConnectAck (ConnectionStatus.Accepted, sessionPresent));
		}
	}
}
