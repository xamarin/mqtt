using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class DisconnectFlow : IProtocolFlow
	{
		readonly IConnectionProvider connectionProvider;
		readonly IRepository<ClientSession> sessionRepository;
		readonly IRepository<ConnectionWill> willRepository;

		public DisconnectFlow (IConnectionProvider connectionProvider, 
			IRepository<ClientSession> sessionRepository, 
			IRepository<ConnectionWill> willRepository)
		{
			this.connectionProvider = connectionProvider;
			this.sessionRepository = sessionRepository;
			this.willRepository = willRepository;
		}

		public Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel)
		{
			if (input.Type != PacketType.Disconnect) {
				return Task.Delay(0);
			}

			var disconnect = input as Disconnect;

			return Task.Run (() => {
				this.willRepository.Delete (w => w.ClientId == clientId);

				var session = this.sessionRepository.Get (s => s.ClientId == clientId);

				if (session == null) {
					throw new ProtocolException (string.Format(Resources.SessionRepository_ClientSessionNotFound, clientId));
				}

				if (session.Clean) {
					this.sessionRepository.Delete (session);
				}

				this.connectionProvider.RemoveConnection (clientId);
			});
		}
	}
}
