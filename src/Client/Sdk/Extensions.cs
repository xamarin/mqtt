using System.Net.Mqtt.Sdk.Flows;
using System.Net.Mqtt.Sdk.Packets;

namespace System.Net.Mqtt.Sdk
{
	internal static class Extensions
	{
		internal static ProtocolFlowType ToFlowType (this MqttPacketType packetType)
		{
			var flowType = default (ProtocolFlowType);

			switch (packetType) {
				case MqttPacketType.Connect:
				case MqttPacketType.ConnectAck:
					flowType = ProtocolFlowType.Connect;
					break;
				case MqttPacketType.Publish:
				case MqttPacketType.PublishRelease:
					flowType = ProtocolFlowType.PublishReceiver;
					break;
				case MqttPacketType.PublishAck:
				case MqttPacketType.PublishReceived:
				case MqttPacketType.PublishComplete:
					flowType = ProtocolFlowType.PublishSender;
					break;
				case MqttPacketType.Subscribe:
				case MqttPacketType.SubscribeAck:
					flowType = ProtocolFlowType.Subscribe;
					break;
				case MqttPacketType.Unsubscribe:
				case MqttPacketType.UnsubscribeAck:
					flowType = ProtocolFlowType.Unsubscribe;
					break;
				case MqttPacketType.PingRequest:
				case MqttPacketType.PingResponse:
					flowType = ProtocolFlowType.Ping;
					break;
				case MqttPacketType.Disconnect:
					flowType = ProtocolFlowType.Disconnect;
					break;
			}

			return flowType;
		}

		internal static MqttQualityOfService GetSupportedQos (this MqttConfiguration configuration, MqttQualityOfService requestedQos)
		{
			return requestedQos > configuration.MaximumQualityOfService ?
				configuration.MaximumQualityOfService :
				requestedQos;
		}

		internal static SubscribeReturnCode ToReturnCode (this MqttQualityOfService qos)
		{
			var returnCode = default (SubscribeReturnCode);

			switch (qos) {
				case MqttQualityOfService.AtMostOnce:
					returnCode = SubscribeReturnCode.MaximumQoS0;
					break;
				case MqttQualityOfService.AtLeastOnce:
					returnCode = SubscribeReturnCode.MaximumQoS1;
					break;
				case MqttQualityOfService.ExactlyOnce:
					returnCode = SubscribeReturnCode.MaximumQoS2;
					break;
			}

			return returnCode;
		}
	}
}