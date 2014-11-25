using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes.Flows
{
	public interface IProtocolFlow
	{
		/// <exception cref="ViolationProtocolException">ViolationProtocolException</exception>
		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task ExecuteAsync (string clientId, IPacket input, ICommunicationContext context);
	}
}
