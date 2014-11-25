using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes
{
	public interface IPacketManager
	{
		/// <exception cref="ConnectProtocolException">ConnectProtocolException</exception>
		/// <exception cref="ViolationProtocolException">ViolationProtocolException</exception>
		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task<IPacket> GetPacketAsync (byte[] bytes);

		/// <exception cref="ConnectProtocolException">ConnectProtocolException</exception>
		/// <exception cref="ViolationProtocolException">ViolationProtocolException</exception>
		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task<byte[]> GetBytesAsync (IPacket packet);
	}
}
