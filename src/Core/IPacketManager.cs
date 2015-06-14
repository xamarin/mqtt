using System.Threading.Tasks;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt
{
	public interface IPacketManager
	{
		/// <exception cref="ProtocolConnectionException">ConnectProtocolException</exception>
		/// <exception cref="ProtocolViolationException">ProtocolViolationException</exception>
		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task<IPacket> GetPacketAsync (byte[] bytes);

		/// <exception cref="ProtocolConnectionException">ConnectProtocolException</exception>
		/// <exception cref="ProtocolViolationException">ProtocolViolationException</exception>
		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task<byte[]> GetBytesAsync (IPacket packet);
	}
}
