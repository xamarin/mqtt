using System.Threading.Tasks;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt
{
	internal interface IPacketManager
	{
		Task<IPacket> GetPacketAsync (byte[] bytes);

		Task<byte[]> GetBytesAsync (IPacket packet);
	}
}
