using System.Threading.Tasks;
using System.Net.Mqtt.Sdk.Packets;
using System.Net.Mqtt.Sdk.Storage;

namespace System.Net.Mqtt.Sdk.Flows
{
	internal interface IPublishSenderFlow : IPublishFlow
	{
		Task SendPublishAsync (string clientId, Publish message, IMqttChannel<IPacket> channel, PendingMessageStatus status = PendingMessageStatus.PendingToSend);
	}
}
