using System.Threading.Tasks;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Storage;

namespace System.Net.Mqtt.Flows
{
	public interface IPublishSenderFlow : IPublishFlow
	{
		Task SendPublishAsync (string clientId, Publish message, IChannel<IPacket> channel, PendingMessageStatus status = PendingMessageStatus.PendingToSend);
	}
}
