using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes.Flows
{
	public class PingFlow : IProtocolFlow
	{
		public async Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel)
		{
			if (input.Type != PacketType.PingRequest)
				return;

			await channel.SendAsync(new PingResponse());
		}
	}
}
