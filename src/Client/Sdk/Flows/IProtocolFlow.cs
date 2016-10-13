using System.Threading.Tasks;
using System.Net.Mqtt.Sdk.Packets;

namespace System.Net.Mqtt.Sdk.Flows
{
	internal interface IProtocolFlow
	{
		Task ExecuteAsync (string clientId, IPacket input, IMqttChannel<IPacket> channel);
	}
}
