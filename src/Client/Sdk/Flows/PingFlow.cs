using System.Threading.Tasks;
using System.Net.Mqtt.Sdk.Packets;

namespace System.Net.Mqtt.Sdk.Flows
{
	internal class PingFlow : IProtocolFlow
	{
		public async Task ExecuteAsync (string clientId, IPacket input, IMqttChannel<IPacket> channel)
		{
			if (input.Type != MqttPacketType.PingRequest) {
				return;
			}

			await channel.SendAsync (new PingResponse ())
				.ConfigureAwait (continueOnCapturedContext: false);
		}
	}
}
