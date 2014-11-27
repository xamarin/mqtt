using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class ClientUnsubscribeFlow : IProtocolFlow
	{
		readonly IRepository<PacketIdentifier> packetIdentifierRepository;

		public ClientUnsubscribeFlow (IRepository<PacketIdentifier> packetIdentifierRepository)
		{
			this.packetIdentifierRepository = packetIdentifierRepository;
		}

		public Task ExecuteAsync (string clientId, IPacket input)
		{
			if (input.Type != PacketType.UnsubscribeAck)
				return Task.Delay(0);
			
			var unsubscribeAck = input as UnsubscribeAck;

			return Task.Run(() => this.packetIdentifierRepository.Delete (i => i.Value == unsubscribeAck.PacketId));
		}
	}
}
