using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes.Flows
{
	public interface IProtocolFlow
	{
		IPacket Apply (IPacket input);
	}
}
