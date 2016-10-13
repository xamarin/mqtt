using System.Threading.Tasks;
using System.Net.Mqtt.Sdk.Packets;

namespace System.Net.Mqtt.Sdk.Flows
{
	internal class ClientSubscribeFlow : IProtocolFlow
	{
		public Task ExecuteAsync (string clientId, IPacket input, IMqttChannel<IPacket> channel)
		{
			return Task.Delay (0);
		}
	}
}
