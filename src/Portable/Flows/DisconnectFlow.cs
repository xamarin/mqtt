using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class DisconnectFlow : IProtocolFlow
	{
		readonly IRepository<ProtocolSession> sessionRepository;
		readonly IRepository<ConnectionWill> willRepository;
		readonly IRepository<ConnectionRefused> connectionRefusedRepository;

		public DisconnectFlow (IRepository<ProtocolSession> sessionRepository, IRepository<ConnectionWill> willRepository, 
			IRepository<ConnectionRefused> connectionRefusedRepository)
		{
			this.sessionRepository = sessionRepository;
			this.willRepository = willRepository;
			this.connectionRefusedRepository = connectionRefusedRepository;
		}

		public IPacket Apply (IPacket input, IProtocolConnection connection)
		{
			if (input.Type != PacketType.Disconnect) {
				var error = string.Format (Resources.ProtocolFlow_InvalidPacketType, input.Type, "Disconnect");

				throw new ProtocolException(error);
			}

			if (this.connectionRefusedRepository.Exist (c => c.ConnectionId == connection.Id)) {
				var error = string.Format (Resources.ProtocolFlow_ConnectionRejected, connection.Id);

				throw new ProtocolException(error);
			}

			if (connection.IsPending)
				throw new ProtocolException (Resources.ProtocolFlow_ConnectRequired);

			this.willRepository.Delete (w => w.ConnectionId == connection.Id);

			connection.Disconnect ();

			return default (IPacket);
		}
	}
}
