using System.Net.Mqtt.Packets;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
    internal interface IPacketChannelFactory
    {
        Task<IChannel<IPacket>> CreateAsync ();

        IChannel<IPacket> Create (IChannel<byte[]> binaryChannel);
    }
}
