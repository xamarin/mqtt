using System.Threading.Tasks;
using Hermes.Diagnostics;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class DisconnectFlow : IProtocolFlow
	{
		static readonly ITracer tracer = Tracer.Get<DisconnectFlow> ();

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

		public async Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel)
		{
			if (input.Type != PacketType.Disconnect) {
				return;
			}

			await Task.Run (() => {
				var disconnect = input as Disconnect;

				tracer.Info (Resources.Tracer_DisconnectFlow_Disconnecting, clientId);

				this.willRepository.Delete (w => w.ClientId == clientId);

				var session = this.sessionRepository.Get (s => s.ClientId == clientId);

				if (session == null) {
					throw new ProtocolException (string.Format(Resources.SessionRepository_ClientSessionNotFound, clientId));
				}

				if (session.Clean) {
					this.sessionRepository.Delete (session);

					tracer.Info (Resources.Tracer_Server_DeletedSessionOnDisconnect, clientId);
				}

				this.connectionProvider.RemoveConnection (clientId);
			});
		}
	}
}
