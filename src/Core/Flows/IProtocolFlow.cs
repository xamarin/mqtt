using System.Threading.Tasks;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Flows
{
	public interface IProtocolFlow
	{
		/// <exception cref="ProtocolViolationException">ProtocolViolationException</exception>
		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel);
	}
}
