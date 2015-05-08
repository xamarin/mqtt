using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes.Flows
{
	public class ClientSubscribeFlow : IProtocolFlow
	{
		public Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel)
		{
			return Task.Delay(0);
		}
	}
}
