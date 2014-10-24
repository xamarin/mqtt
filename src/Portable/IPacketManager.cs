using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes
{
	public interface IPacketManager
	{
		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task ManageAsync (byte[] packet);

		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task ManageAsync (IPacket packet);
	}
}
