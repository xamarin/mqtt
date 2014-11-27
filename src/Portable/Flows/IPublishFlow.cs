using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes.Flows
{
	public interface IPublishFlow : IProtocolFlow
	{
		Task SendPublishAsync (string clientId, Publish message, IChannel<IPacket> channel);

		Task SendAckAsync (string clientId, IFlowPacket ack, IChannel<IPacket> channel);
	}
}
