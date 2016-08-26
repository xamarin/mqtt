using System.Threading.Tasks;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Storage;

namespace System.Net.Mqtt.Flows
{
	internal interface IPublishFlow : IProtocolFlow
	{
		Task SendAckAsync (string clientId, IFlowPacket ack, IMqttChannel<IPacket> channel, PendingMessageStatus status = PendingMessageStatus.PendingToSend);
	}
}
