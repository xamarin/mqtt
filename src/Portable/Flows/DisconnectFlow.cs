using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Properties;
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
			var disconnect = input as Disconnect;

			if (disconnect == null) {
				var error = string.Format (Resources.ProtocolFlow_InvalidPacketType, input.Type, "Disconnect");

				throw new ProtocolException(error);
			}

			return Task.Run (() => {
				this.willRepository.Delete (w => w.ClientId == clientId);
				this.clientManager.RemoveClient (clientId);

				channel.Close ();
			});
		}
	}
}
