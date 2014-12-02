using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes
{
	public interface IPublishDispatcher
	{
		Task DispatchAsync (Publish publish);
	}
}
