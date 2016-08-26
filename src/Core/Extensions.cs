using System.Net.Mqtt.Flows;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt
{
	internal static class Extensions
	{
		internal static ProtocolFlowType ToFlowType (this PacketType packetType)
		{
			var flowType = default (ProtocolFlowType);

			switch (packetType) {
				case PacketType.Connect:
				case PacketType.ConnectAck:
					flowType = ProtocolFlowType.Connect;
					break;
				case PacketType.Publish:
				case PacketType.PublishRelease:
					flowType = ProtocolFlowType.PublishReceiver;
					break;
				case PacketType.PublishAck:
				case PacketType.PublishReceived:
				case PacketType.PublishComplete:
					flowType = ProtocolFlowType.PublishSender;
					break;
				case PacketType.Subscribe:
				case PacketType.SubscribeAck:
					flowType = ProtocolFlowType.Subscribe;
					break;
				case PacketType.Unsubscribe:
				case PacketType.UnsubscribeAck:
					flowType = ProtocolFlowType.Unsubscribe;
					break;
				case PacketType.PingRequest:
				case PacketType.PingResponse:
					flowType = ProtocolFlowType.Ping;
					break;
				case PacketType.Disconnect:
					flowType = ProtocolFlowType.Disconnect;
					break;
			}

			return flowType;
		}

		internal static QualityOfService GetSupportedQos (this ProtocolConfiguration configuration, QualityOfService requestedQos)
		{
			return requestedQos > configuration.MaximumQualityOfService ?
				configuration.MaximumQualityOfService :
				requestedQos;
		}

		internal static SubscribeReturnCode ToReturnCode (this QualityOfService qos)
		{
			var returnCode = default (SubscribeReturnCode);

			switch (qos) {
				case QualityOfService.AtMostOnce:
					returnCode = SubscribeReturnCode.MaximumQoS0;
					break;
				case QualityOfService.AtLeastOnce:
					returnCode = SubscribeReturnCode.MaximumQoS1;
					break;
				case QualityOfService.ExactlyOnce:
					returnCode = SubscribeReturnCode.MaximumQoS2;
					break;
			}

			return returnCode;
		}
	}
}