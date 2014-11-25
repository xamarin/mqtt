using Hermes.Packets;

namespace Hermes.Flows
{
	public interface IProtocolFlowProvider
	{
		/// <exception cref="ProtocolException">ProtocolException</exception>
		IProtocolFlow GetFlow (PacketType packetType);
	}
}
