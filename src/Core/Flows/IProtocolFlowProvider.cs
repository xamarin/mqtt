using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Flows
{
	internal interface IProtocolFlowProvider
	{
		/// <exception cref="ProtocolException">ProtocolException</exception>
		IProtocolFlow GetFlow (MqttPacketType packetType);

		T GetFlow<T> () where T : class, IProtocolFlow;
	}
}
