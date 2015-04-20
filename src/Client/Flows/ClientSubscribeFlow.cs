using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class ClientSubscribeFlow : IProtocolFlow
	{
		readonly IRepository<PacketIdentifier> packetIdentifierRepository;

		public ClientSubscribeFlow (IRepository<PacketIdentifier> packetIdentifierRepository)
		{
			this.packetIdentifierRepository = packetIdentifierRepository;
		}

		public Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel)
		{
			if (input.Type != PacketType.SubscribeAck) {
				return Task.Delay(0);
			}

			var subscribeAck = input as SubscribeAck;

			return Task.Run(() => this.packetIdentifierRepository.Delete (i => i.Value == subscribeAck.PacketId));
		}
	}
}
