using System.Net.Mqtt.Sdk.Packets;

namespace System.Net.Mqtt.Sdk.Flows
{
	internal interface IProtocolFlowProvider
	{
		IProtocolFlow GetFlow (MqttPacketType packetType);

		T GetFlow<T> () where T : class, IProtocolFlow;
	}
}
