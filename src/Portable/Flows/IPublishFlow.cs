using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Storage;

namespace Hermes.Flows
{
	public interface IPublishFlow : IProtocolFlow
	{
		Task SendAckAsync (string clientId, IFlowPacket ack, PendingMessageStatus status = PendingMessageStatus.PendingToSend);
	}
}
