using System.Threading.Tasks;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Flows
{
	internal interface IProtocolFlow
	{
		Task ExecuteAsync (string clientId, IPacket input, IMqttChannel<IPacket> channel);
	}
}
