using System.Threading.Tasks;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Flows
{
	internal class PingFlow : IProtocolFlow
	{
		public async Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel)
		{
			if (input.Type != PacketType.PingRequest) {
				return;
			}

			await channel.SendAsync (new PingResponse ())
				.ConfigureAwait (continueOnCapturedContext: false);
		}
	}
}
