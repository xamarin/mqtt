using System;
using System.Collections.Generic;
using Hermes.Messages;

namespace Hermes.Formatters
{
	public class PublishFormatter : Formatter<Publish>
	{
		public PublishFormatter (IChannel<IMessage> reader, IChannel<byte[]> writer)
			: base(reader, writer)
		{
		}

		protected override Publish Format (byte[] packet)
		{
			var remainingLengthBytesLength = 0;
			var remainingLength = ProtocolEncoding.DecodeRemainingLength (packet, out remainingLengthBytesLength);

			var packetFlags = packet.Byte (0).Bits(5, 4);

			var retainFlag = packetFlags.IsSet (0);
			var qos = (QualityOfService)packetFlags.Bits (6, 2);
			var duplicated = packetFlags.IsSet (3);

			var topicStartIndex = remainingLengthBytesLength + 1;
			var nextIndex = 0;
			var topic = packet.GetString (topicStartIndex, out nextIndex);
			var variableHeaderLength = topic.Length + 2;
			var messageId = default (ushort?);

			if (qos != QualityOfService.AtMostOnce) {
				messageId = packet.Bytes (nextIndex, 2).ToUInt16 ();
				variableHeaderLength += 2;
			}

			var publish = new Publish (qos, retainFlag, topic, messageId, duplicated);

			if (remainingLength > variableHeaderLength) {
				publish.Payload = packet.Bytes (variableHeaderLength + 2);
			}

			return publish;
		}

		protected override byte[] Format (Publish message)
		{
			var packet = new List<byte> ();

			var variableHeader = this.GetVariableHeader (message);
			var payloadLength = message.Payload == null ? 0 : message.Payload.Length;
			var remainingLength = ProtocolEncoding.EncodeRemainingLength (variableHeader.Length + payloadLength);
			var fixedHeader = this.GetFixedHeader (message, remainingLength);

			packet.AddRange (fixedHeader);
			packet.AddRange (variableHeader);

			if (message.Payload != null) {
				packet.AddRange (message.Payload);
			}

			return packet.ToArray();
		}

		private byte[] GetFixedHeader(Publish message, byte[] remainingLength)
		{
			var fixedHeader = new List<byte> ();

			var retain = Convert.ToInt32 (message.Retain);
			var qos = Convert.ToInt32(message.QualityOfService);
			var duplicated = Convert.ToInt32 (message.DuplicatedDelivery);

			qos <<= 1;
			duplicated <<= 3;

			var flags = Convert.ToByte(retain | qos | duplicated);
			var type = Convert.ToInt32(MessageType.Publish) << 4;

			var fixedHeaderByte1 = Convert.ToByte(flags | type);

			fixedHeader.Add (fixedHeaderByte1);
			fixedHeader.AddRange (remainingLength);

			return fixedHeader.ToArray();
		}

		private byte[] GetVariableHeader(Publish message)
		{
			var variableHeader = new List<byte> ();

			var topicBytes = ProtocolEncoding.EncodeString(message.Topic);

			variableHeader.AddRange (topicBytes);

			if (message.MessageId.HasValue) {
				var messageIdBytes = ProtocolEncoding.EncodeBigEndian(message.MessageId.Value);

				variableHeader.AddRange (messageIdBytes);
			}

			return variableHeader.ToArray();
		}
	}
}
