using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes.Flows
{
	//TODO: Pending to add Session State entity and behavior
	//TODO: Pending to add support for this:  If the ClientId represents a Client already connected to the Server then the Server MUST disconnect the existing Client
	public class ConnectFlow : IProtocolFlow
	{
		readonly IRepository<ProtocolSession> sessionRepository;
		readonly IRepository<ConnectionWill> willRepository;
		readonly IRepository<ConnectionRefused> connectionRefusedRepository;

		public ConnectFlow (IRepository<ProtocolSession> sessionRepository, IRepository<ConnectionWill> willRepository, 
			IRepository<ConnectionRefused> connectionRefusedRepository)
		{
			this.sessionRepository = sessionRepository;
			this.willRepository = willRepository;
			this.connectionRefusedRepository = connectionRefusedRepository;
		}

		public IPacket Apply (IPacket input, IProtocolConnection connection)
		{
			if (input.Type != PacketType.Connect && input.Type != PacketType.ConnectAck) {
				var error = string.Format (Resources.ProtocolFlow_InvalidPacketType, input.Type, "Connect");

				throw new ProtocolException(error);
			}
				
			//TODO: Find a way of encapsulating this logic to avoid repeating it in each flow
			if (this.connectionRefusedRepository.Exist (c => c.ConnectionId == connection.Id)) {
				var error = string.Format (Resources.ProtocolFlow_ConnectionRejected, connection.Id);

				throw new ProtocolException(error);

			}

			if (!connection.IsPending)
				throw new ViolationProtocolException (Resources.ConnectFlow_SecondConnectNotAllowed);

			if(input.Type == PacketType.ConnectAck)
				return default(IPacket);

			//TODO: Add exception handling to prevent any repository error

			var connect = input as Connect;
			var session = this.sessionRepository.Get (s => s.ClientId == connect.ClientId);
			var sessionPresent = connect.CleanSession ? false : session != null;

			if (connect.CleanSession && session != null) {
				this.sessionRepository.Delete(session);
			}

			if (session == null) {
				session = new ProtocolSession { ClientId = connect.ClientId, Clean = connect.CleanSession };

				this.sessionRepository.Create (session);
			}

			if (connect.Will != null) {
				var connectionWill = new ConnectionWill { ConnectionId = connection.Id, Will = connect.Will };

				this.willRepository.Create (connectionWill);
			}

			connection.Confirm (connect.ClientId);

			return new ConnectAck (ConnectionStatus.Accepted, sessionPresent);
		}

		public IPacket GetError (ConnectionStatus status)
		{
			if (status == ConnectionStatus.Accepted)
				throw new ProtocolException (Resources.ConnectFlow_NoErrorCodeDetected);

			return new ConnectAck (status, existingSession: false);
		}
	}
}
