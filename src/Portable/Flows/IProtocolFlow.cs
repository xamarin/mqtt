using Hermes.Packets;

namespace Hermes.Flows
{
	public interface IProtocolFlow
	{
		/// <exception cref="ViolationProtocolException">ViolationProtocolException</exception>
		/// /// <exception cref="ProtocolException">ProtocolException</exception>
		IPacket Apply (IPacket input, IProtocolConnection connection);
	}
}
