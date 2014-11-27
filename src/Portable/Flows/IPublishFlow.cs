using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes.Flows
{
	public interface IPublishFlow : IProtocolFlow
	{
		Task SendAckAsync (string clientId, IFlowPacket ack, bool isPending = false);
	}
}
