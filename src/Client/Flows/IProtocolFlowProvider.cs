using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Flows
{
	internal interface IProtocolFlowProvider
	{
		IProtocolFlow GetFlow (MqttPacketType packetType);

		T GetFlow<T> () where T : class, IProtocolFlow;
	}
}
