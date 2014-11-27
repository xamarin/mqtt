using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Storage;

namespace Hermes.Flows
{
	public interface IPublishSenderFlow : IPublishFlow
	{
		Task SendPublishAsync (string clientId, Publish message, PendingMessageStatus status = PendingMessageStatus.PendingToSend);
	}
}
