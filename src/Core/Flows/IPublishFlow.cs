using System.Threading.Tasks;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Storage;

namespace System.Net.Mqtt.Flows
{
	public interface IPublishFlow : IProtocolFlow
	{
		Task SendAckAsync (string clientId, IFlowPacket ack, IChannel<IPacket> channel, PendingMessageStatus status = PendingMessageStatus.PendingToSend);
	}
}
