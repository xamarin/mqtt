using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes
{
	public interface IPacketManager
	{
		/// <exception cref="ConnectProtocolException">ConnectProtocolException</exception>
		/// <exception cref="ViolationProtocolException">ViolationProtocolException</exception>
		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task<IPacket> GetAsync (byte[] bytes);

		/// <exception cref="ConnectProtocolException">ConnectProtocolException</exception>
		/// <exception cref="ViolationProtocolException">ViolationProtocolException</exception>
		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task<byte[]> GetAsync (IPacket packet);
	}
}
