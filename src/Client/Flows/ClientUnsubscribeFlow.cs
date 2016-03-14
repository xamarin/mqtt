using System.Threading.Tasks;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Flows
{
	internal class ClientUnsubscribeFlow : IProtocolFlow
	{
		public Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel)
		{
			return Task.Delay (0);
		}
	}
}
