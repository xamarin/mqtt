using System.Collections.Generic;
using System.Net.Mqtt.Sdk.Packets;

namespace System.Net.Mqtt.Sdk.Formatters
{
	internal class PublishFormatter : Formatter<Publish>
	{
		readonly IMqttTopicEvaluator topicEvaluator;

		public PublishFormatter (IMqttTopicEvaluator topicEvaluator)
		{
			this.topicEvaluator = topicEvaluator;
		}

		public override MqttPacketType PacketType { get { return Packets.MqttPacketType.Publish; } }

		protected override Publish Read (byte[] bytes)
		{
			var remainingLengthBytesLength = 0;
			var remainingLength = MqttProtocol.Encoding.DecodeRemainingLength (bytes, out remainingLengthBytesLength);

			var packetFlags = bytes.Byte (0).Bits(5, 4);

			if (packetFlags.Bits (6, 2) == 0x03)
				throw new MqttException  (Properties.Resources.Formatter_InvalidQualityOfService);

			var qos = (MqttQualityOfService)packetFlags.Bits (6, 2);
			var duplicated = packetFlags.IsSet (3);

			if (qos == MqttQualityOfService.AtMostOnce && duplicated)
				throw new MqttException  (Properties.Resources.PublishFormatter_InvalidDuplicatedWithQoSZero);

			var retainFlag = packetFlags.IsSet (0);

			var topicStartIndex = 1 + remainingLengthBytesLength;
			var nextIndex = 0;
			var topic = bytes.GetString (topicStartIndex, out nextIndex);

			if (!topicEvaluator.IsValidTopicName (topic)) {
				var error = string.Format (Properties.Resources.PublishFormatter_InvalidTopicName, topic);

				throw new MqttException (error);
			}

			var variableHeaderLength = topic.Length + 2;
			var packetId = default (ushort?);

			if (qos != MqttQualityOfService.AtMostOnce) {
				packetId = bytes.Bytes (nextIndex, 2).ToUInt16 ();
				variableHeaderLength += 2;
			}

			var publish = new Publish (topic, qos, retainFlag, duplicated, packetId);

			if (remainingLength > variableHeaderLength) {
				var payloadStartIndex = 1 + remainingLengthBytesLength + variableHeaderLength;

				publish.Payload = bytes.Bytes (payloadStartIndex);
			}

			return publish;
		}

		protected override byte[] Write (Publish packet)
		{
			var bytes = new List<byte> ();

			var variableHeader = GetVariableHeader (packet);
			var payloadLength = packet.Payload == null ? 0 : packet.Payload.Length;
			var remainingLength = MqttProtocol.Encoding.EncodeRemainingLength (variableHeader.Length + payloadLength);
			var fixedHeader = GetFixedHeader (packet, remainingLength);

			bytes.AddRange (fixedHeader);
			bytes.AddRange (variableHeader);

			if (packet.Payload != null) {
				bytes.AddRange (packet.Payload);
			}

			return bytes.ToArray ();
		}

		byte[] GetFixedHeader (Publish packet, byte[] remainingLength)
		{
			if (packet.QualityOfService == MqttQualityOfService.AtMostOnce && packet.Duplicated)
				throw new MqttException  (Properties.Resources.PublishFormatter_InvalidDuplicatedWithQoSZero);

			var fixedHeader = new List<byte> ();

			var retain = Convert.ToInt32 (packet.Retain);
			var qos = Convert.ToInt32(packet.QualityOfService);
			var duplicated = Convert.ToInt32 (packet.Duplicated);

			qos <<= 1;
			duplicated <<= 3;

			var flags = Convert.ToByte(retain | qos | duplicated);
			var type = Convert.ToInt32(MqttPacketType.Publish) << 4;

			var fixedHeaderByte1 = Convert.ToByte(flags | type);

			fixedHeader.Add (fixedHeaderByte1);
			fixedHeader.AddRange (remainingLength);

			return fixedHeader.ToArray ();
		}

		byte[] GetVariableHeader (Publish packet)
		{
			if (!topicEvaluator.IsValidTopicName (packet.Topic))
				throw new MqttException  (Properties.Resources.PublishFormatter_InvalidTopicName);

			if (packet.PacketId.HasValue && packet.QualityOfService == MqttQualityOfService.AtMostOnce)
				throw new MqttException  (Properties.Resources.PublishFormatter_InvalidPacketId);

			if (!packet.PacketId.HasValue && packet.QualityOfService != MqttQualityOfService.AtMostOnce)
				throw new MqttException  (Properties.Resources.PublishFormatter_PacketIdRequired);

			var variableHeader = new List<byte> ();

			var topicBytes = MqttProtocol.Encoding.EncodeString(packet.Topic);

			variableHeader.AddRange (topicBytes);

			if (packet.PacketId.HasValue) {
				var packetIdBytes = MqttProtocol.Encoding.EncodeInteger(packet.PacketId.Value);

				variableHeader.AddRange (packetIdBytes);
			}

			return variableHeader.ToArray ();
		}
	}
}
