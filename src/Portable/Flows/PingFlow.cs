using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Properties;

namespace Hermes.Flows
{
	public class PingFlow : IProtocolFlow
	{
		readonly IConnectionProvider connectionProvider;

		public PingFlow (IConnectionProvider connectionProvider)
		{
			this.connectionProvider = connectionProvider;
		}

		public async Task ExecuteAsync (string clientId, IPacket input)
		{
			if (input.Type == PacketType.PingResponse)
				return;

			var ping = input as PingRequest;

			if (ping == null) {
				var error = string.Format (Resources.ProtocolFlow_InvalidPacketType, input.Type, "Ping");

				throw new ProtocolException(error);
			}

			var channel = this.connectionProvider.GetConnection (clientId);

			await channel.SendAsync(new PingResponse());
		}
	}
}
