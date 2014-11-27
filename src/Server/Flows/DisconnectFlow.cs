using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class DisconnectFlow : IProtocolFlow
	{
		readonly IClientManager clientManager;
		readonly IRepository<ConnectionWill> willRepository;

		public DisconnectFlow (IClientManager clientManager, IRepository<ConnectionWill> willRepository)
		{
			this.clientManager = clientManager;
			this.willRepository = willRepository;
		}

		public Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel)
		{
			if (input.Type != PacketType.Disconnect)
				return Task.Delay(0);

			var disconnect = input as Disconnect;

			return Task.Run (() => {
				this.willRepository.Delete (w => w.ClientId == clientId);
				this.clientManager.RemoveClient (clientId);

				channel.Close ();
			});
		}
	}
}
