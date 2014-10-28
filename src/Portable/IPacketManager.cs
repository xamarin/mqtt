using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes
{
	public interface IPacketManager
	{
		/// <exception cref="ConnectProtocolException">ConnectProtocolException</exception>
		/// <exception cref="ViolationProtocolException">ViolationProtocolException</exception>
		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task ManageAsync (byte[] packet);

		/// <exception cref="ConnectProtocolException">ConnectProtocolException</exception>
		/// <exception cref="ViolationProtocolException">ViolationProtocolException</exception>
		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task ManageAsync (IPacket packet);

		//TODO: Proposal to change IPacketManager (this will alow to use it from PacketChannel
		//public interface IPacketConverter
		//{
			//Task<IPacket> ConvertAsync (byte[] packet);

			//Task<byte[]> ConvertAsync (IPacket packet);
		//}
	}
}
