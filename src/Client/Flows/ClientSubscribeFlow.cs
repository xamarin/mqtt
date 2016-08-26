using System.Threading.Tasks;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Flows
{
	internal class ClientSubscribeFlow : IProtocolFlow
	{
		public Task ExecuteAsync (string clientId, IPacket input, IMqttChannel<IPacket> channel)
		{
			return Task.Delay (0);
		}
	}
}
