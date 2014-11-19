using System;
using System.Collections.Generic;
using Hermes.Packets;
using Hermes.Properties;

namespace Hermes.Formatters
{
	public class PublishFormatter : Formatter<Publish>
	{
		readonly ITopicEvaluator topicEvaluator;

		public PublishFormatter (ITopicEvaluator topicEvaluator)
		{
			this.topicEvaluator = topicEvaluator;
		}

		public override PacketType PacketType { get { return Packets.PacketType.Publish; } }

		protected override Publish Read (byte[] bytes)
		{
			var remainingLengthBytesLength = 0;
			var remainingLength = Protocol.Encoding.DecodeRemainingLength (bytes, out remainingLengthBytesLength);

			var packetFlags = bytes.Byte (0).Bits(5, 4);

			if (packetFlags.Bits (6, 2) == 0x03)
				throw new ProtocolException (Resources.Formatter_InvalidQualityOfService);

			var qos = (QualityOfService)packetFlags.Bits (6, 2);
			var duplicated = packetFlags.IsSet (3);

			if (qos == QualityOfService.AtMostOnce && duplicated)
				throw new ProtocolException (Resources.PublishFormatter_InvalidDuplicatedWithQoSZero);

			var retainFlag = packetFlags.IsSet (0);

			var topicStartIndex = 1 + remainingLengthBytesLength;
			var nextIndex = 0;
			var topic = bytes.GetString (topicStartIndex, out nextIndex);

			if (!this.topicEvaluator.IsValidTopicName (topic)) {
				var error = string.Format(Resources.PublishFormatter_InvalidTopicName, topic);

				throw new ProtocolException (error);
			}

			var variableHeaderLength = topic.Length + 2;
			var packetId = default (ushort?);

			if (qos != QualityOfService.AtMostOnce) {
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

			var variableHeader = this.GetVariableHeader (packet);
			var payloadLength = packet.Payload == null ? 0 : packet.Payload.Length;
			var remainingLength = Protocol.Encoding.EncodeRemainingLength (variableHeader.Length + payloadLength);
			var fixedHeader = this.GetFixedHeader (packet, remainingLength);

			bytes.AddRange (fixedHeader);
			bytes.AddRange (variableHeader);

			if (packet.Payload != null) {
				bytes.AddRange (packet.Payload);
			}

			return bytes.ToArray();
		}

		private byte[] GetFixedHeader(Publish packet, byte[] remainingLength)
		{
			if (packet.QualityOfService == QualityOfService.AtMostOnce && packet.DuplicatedDelivery)
				throw new ProtocolException (Resources.PublishFormatter_InvalidDuplicatedWithQoSZero);

			var fixedHeader = new List<byte> ();

			var retain = Convert.ToInt32 (packet.Retain);
			var qos = Convert.ToInt32(packet.QualityOfService);
			var duplicated = Convert.ToInt32 (packet.DuplicatedDelivery);

			qos <<= 1;
			duplicated <<= 3;

			var flags = Convert.ToByte(retain | qos | duplicated);
			var type = Convert.ToInt32(PacketType.Publish) << 4;

			var fixedHeaderByte1 = Convert.ToByte(flags | type);

			fixedHeader.Add (fixedHeaderByte1);
			fixedHeader.AddRange (remainingLength);

			return fixedHeader.ToArray();
		}

		private byte[] GetVariableHeader(Publish packet)
		{
			if (!this.topicEvaluator.IsValidTopicName (packet.Topic))
				throw new ProtocolException (Resources.PublishFormatter_InvalidTopicName);

			if (packet.PacketId.HasValue && packet.QualityOfService == QualityOfService.AtMostOnce)
					throw new ProtocolException (Resources.PublishFormatter_InvalidPacketId);

			if(!packet.PacketId.HasValue && packet.QualityOfService != QualityOfService.AtMostOnce)
				throw new ProtocolException (Resources.PublishFormatter_PacketIdRequired);

			var variableHeader = new List<byte> ();

			var topicBytes = Protocol.Encoding.EncodeString(packet.Topic);

			variableHeader.AddRange (topicBytes);

			if (packet.PacketId.HasValue) {
				var packetIdBytes = Protocol.Encoding.EncodeBigEndian(packet.PacketId.Value);

				variableHeader.AddRange (packetIdBytes);
			}

			return variableHeader.ToArray();
		}
	}
}
