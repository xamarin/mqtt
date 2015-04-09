using System;
using Hermes.Flows;
using Hermes.Packets;

namespace Hermes
{
	public static class Extensions
	{
		public static ProtocolFlowType ToFlowType(this PacketType packetType)
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

		public static QualityOfService GetSupportedQos(this ProtocolConfiguration configuration, QualityOfService requestedQos)
		{
			return requestedQos > configuration.MaximumQualityOfService ?
				configuration.MaximumQualityOfService : 
				requestedQos;
		}

		public static SubscribeReturnCode ToReturnCode(this QualityOfService qos)
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

namespace Hermes.Storage
{
	public static class StorageExtensions
	{
		public static ushort? GetPacketIdentifier(this IRepository<PacketIdentifier> repository, QualityOfService qos)
		{
			var packetId = default (ushort?);

			if(qos != QualityOfService.AtMostOnce) {
				packetId = repository.GetUnusedPacketIdentifier (new Random ());
			}

			return packetId;
		}

		public static ushort GetUnusedPacketIdentifier(this IRepository<PacketIdentifier> repository, Random random)
		{
			var packetId = (ushort)random.Next (1, ushort.MaxValue);

			if (repository.Exist (i => i.Value == packetId)) {
				packetId = repository.GetUnusedPacketIdentifier (random);
			}

			repository.Create (new PacketIdentifier { Value = packetId });

			return packetId;
		}
	}
}
