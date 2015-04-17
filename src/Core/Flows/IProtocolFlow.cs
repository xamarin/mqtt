using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes.Flows
{
	public interface IProtocolFlow
	{
		/// <exception cref="ProtocolViolationException">ProtocolViolationException</exception>
		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel);
	}
}
