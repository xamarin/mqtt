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

		/// <exception cref="ProtocolConnectionException">ConnectProtocolException</exception>
		/// <exception cref="ProtocolViolationException">ProtocolViolationException</exception>
		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task<IPacket> FormatAsync (byte[] bytes);

		/// <exception cref="ProtocolConnectionException">ConnectProtocolException</exception>
		/// <exception cref="ProtocolViolationException">ProtocolViolationException</exception>
		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task<byte[]> FormatAsync (IPacket packet);
	}
}
