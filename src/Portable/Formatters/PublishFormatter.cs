using System;
using System.Collections.Generic;
using System.Text;
using Hermes.Messages;
using Hermes.Properties;

namespace Hermes.Formatters
{
	public class PublishFormatter : Formatter<Publish>
	{
		public PublishFormatter (IChannel<IMessage> reader, IChannel<byte[]> writer)
			: base(reader, writer)
		{
		}

		public override MessageType MessageType { get { return Messages.MessageType.Publish; } }

		protected override Publish Read (byte[] packet)
		{
			var remainingLengthBytesLength = 0;
			var remainingLength = Protocol.Encoding.DecodeRemainingLength (packet, out remainingLengthBytesLength);

			var packetFlags = packet.Byte (0).Bits(5, 4);

			if (packetFlags.Bits (6, 2) == 0x03)
				throw new ProtocolException (Resources.Formatter_InvalidQualityOfService);

			var qos = (QualityOfService)packetFlags.Bits (6, 2);
			var duplicated = packetFlags.IsSet (3);

			if (qos == QualityOfService.AtMostOnce && duplicated)
				throw new ProtocolException (Resources.PublishFormatter_InvalidDuplicatedWithQoSZero);

			var retainFlag = packetFlags.IsSet (0);

			var topicStartIndex = remainingLengthBytesLength + 1;
			var nextIndex = 0;
			var topic = packet.GetString (topicStartIndex, out nextIndex);

			if (!this.IsValidTopicName (topic))
				throw new ProtocolException (Resources.PublishFormatter_InvalidTopicName);

			var variableHeaderLength = topic.Length + 2;
			var messageId = default (ushort?);

			if (qos != QualityOfService.AtMostOnce) {
				messageId = packet.Bytes (nextIndex, 2).ToUInt16 ();
				variableHeaderLength += 2;
			}

			var publish = new Publish (topic, qos, retainFlag, duplicated, messageId);

			if (remainingLength > variableHeaderLength) {
				publish.Payload = packet.Bytes (variableHeaderLength + 2);
			}

			return publish;
		}

		protected override byte[] Write (Publish message)
		{
			var packet = new List<byte> ();

			var variableHeader = this.GetVariableHeader (message);
			var payloadLength = message.Payload == null ? 0 : message.Payload.Length;
			var remainingLength = Protocol.Encoding.EncodeRemainingLength (variableHeader.Length + payloadLength);
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
			if (message.QualityOfService == QualityOfService.AtMostOnce && message.DuplicatedDelivery)
				throw new ProtocolException (Resources.PublishFormatter_InvalidDuplicatedWithQoSZero);

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
			if (!this.IsValidTopicName (message.Topic))
				throw new ProtocolException (Resources.PublishFormatter_InvalidTopicName);

			if (message.MessageId.HasValue && message.QualityOfService == QualityOfService.AtMostOnce)
					throw new ProtocolException (Resources.PublishFormatter_InvalidMessageId);

			if(!message.MessageId.HasValue && message.QualityOfService != QualityOfService.AtMostOnce)
				throw new ProtocolException (Resources.PublishFormatter_MessageIdRequired);

			var variableHeader = new List<byte> ();

			var topicBytes = Protocol.Encoding.EncodeString(message.Topic);

			variableHeader.AddRange (topicBytes);

			if (message.MessageId.HasValue) {
				var messageIdBytes = Protocol.Encoding.EncodeBigEndian(message.MessageId.Value);

				variableHeader.AddRange (messageIdBytes);
			}

			return variableHeader.ToArray();
		}

		private bool IsValidTopicName (string topic)
		{
			return !string.IsNullOrEmpty (topic) &&
				Encoding.UTF8.GetBytes(topic).Length <= 65536 &&
				!topic.Contains ("#") &&
				!topic.Contains ("+");
		}
	}
}
