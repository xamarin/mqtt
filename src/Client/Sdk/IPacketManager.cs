using System.Threading.Tasks;
using System.Net.Mqtt.Sdk.Packets;

namespace System.Net.Mqtt.Sdk
{
	internal interface IPacketManager
	{
		Task<IPacket> GetPacketAsync (byte[] bytes);

		Task<byte[]> GetBytesAsync (IPacket packet);
	}
}
