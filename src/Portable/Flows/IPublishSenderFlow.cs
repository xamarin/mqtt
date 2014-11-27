using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes.Flows
{
	public interface IPublishSenderFlow : IPublishFlow
	{
		Task SendPublishAsync (string clientId, Publish message, IChannel<IPacket> channel);
	}
}
