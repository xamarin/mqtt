using System.Threading.Tasks;
using Hermes.Messages;

namespace Hermes.Flows
{
	public interface IProtocolFlow
	{
		IMessage Apply (IMessage input);
	}
}
