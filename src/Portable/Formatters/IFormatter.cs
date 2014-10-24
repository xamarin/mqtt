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

		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task ReadAsync (byte[] bytes);

		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task WriteAsync (IPacket packet);
	}
}
