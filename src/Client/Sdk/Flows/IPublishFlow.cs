using System.Threading.Tasks;
using System.Net.Mqtt.Sdk.Packets;
using System.Net.Mqtt.Sdk.Storage;

namespace System.Net.Mqtt.Sdk.Flows
{
	internal interface IPublishFlow : IProtocolFlow
	{
		Task SendAckAsync (string clientId, IFlowPacket ack, IMqttChannel<IPacket> channel, PendingMessageStatus status = PendingMessageStatus.PendingToSend);
	}
}
