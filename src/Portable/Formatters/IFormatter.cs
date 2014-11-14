using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes.Formatters
{
	public interface IFormatter
	{
		/// <summary>
		/// Gets the type of packet that this formatter support.
		/// </summary>
		PacketType PacketType { get; }

		/// <exception cref="ConnectProtocolException">ConnectProtocolException</exception>
		/// <exception cref="ViolationProtocolException">ViolationProtocolException</exception>
		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task<IPacket> FormatAsync (byte[] bytes);

		/// <exception cref="ConnectProtocolException">ConnectProtocolException</exception>
		/// <exception cref="ViolationProtocolException">ViolationProtocolException</exception>
		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task<byte[]> FormatAsync (IPacket packet);
	}
}
