using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Properties;

namespace Hermes.Flows
{
	public class PingFlow : IProtocolFlow
	{
		public async Task ExecuteAsync (string clientId, IPacket input, ICommunicationContext context)
		{
			if (input.Type == PacketType.PingResponse)
				return;

			var ping = input as PingRequest;

			if (ping == null) {
				var error = string.Format (Resources.ProtocolFlow_InvalidPacketType, input.Type, "Ping");

				throw new ProtocolException(error);
			}

			await context.PushDeliveryAsync(new PingResponse());
		}
	}
}
