using Hermes.Packets;

namespace Hermes.Flows
{
	public interface IProtocolFlowProvider
	{
		IProtocolFlow Get (PacketType packetType);

		ProtocolFlowType GetFlowType (PacketType packetType);
	}
}
