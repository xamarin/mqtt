using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class PingFlow : IProtocolFlow
	{
		readonly IRepository<ConnectionRefused> connectionRefusedRepository;

		public PingFlow (IRepository<ConnectionRefused> connectionRefusedRepository)
		{
			this.connectionRefusedRepository = connectionRefusedRepository;
		}

		public IPacket Apply (IPacket input, IProtocolConnection connection)
		{
			if (input.Type != PacketType.PingRequest && input.Type != PacketType.PingResponse) {
				var error = string.Format (Resources.ProtocolFlow_InvalidPacketType, input.Type, "Ping");

				throw new ProtocolException(error);
			}

			//TODO: Find a way of encapsulating this logic to avoid repeating it in each flow
			if (this.connectionRefusedRepository.Exist (c => c.ConnectionId == connection.Id)) {
				var error = string.Format (Resources.ProtocolFlow_ConnectionRejected, connection.Id);

				throw new ProtocolException(error);
			}

			if (connection.IsPending)
				throw new ProtocolException (Resources.ProtocolFlow_ConnectRequired);

			if(input.Type == PacketType.PingResponse)
				return default(IPacket);

			return new PingResponse ();
		}
	}
}
