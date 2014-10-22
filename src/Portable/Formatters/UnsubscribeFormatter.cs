using System;
using System.Collections.Generic;
using Hermes.Messages;

namespace Hermes.Formatters
{
	public class UnsubscribeFormatter : Formatter<Unsubscribe>
	{
		public UnsubscribeFormatter (IChannel<IMessage> reader, IChannel<byte[]> writer)
			: base(reader, writer)
		{
		}

		public override MessageType MessageType { get { return Messages.MessageType.Unsubscribe; } }

		protected override Unsubscribe Read (byte[] packet)
		{
			var remainingLengthBytesLength = 0;
			var remainingLength = Protocol.Encoding.DecodeRemainingLength (packet, out remainingLengthBytesLength);

			var packetIdentifierStartIndex = remainingLengthBytesLength + 1;
			var packetIdentifier = packet.Bytes (packetIdentifierStartIndex, 2).ToUInt16();

			var index = 1 + remainingLengthBytesLength + 2;
			var topics = new List<string> ();

			do {
				var topic = packet.GetString (index, out index);

				topics.Add (topic);
			} while (packet.Length - index + 1 >= 2);

			return new Unsubscribe (packetIdentifier, topics.ToArray());
		}

		protected override byte[] Write (Unsubscribe message)
		{
			var packet = new List<byte> ();

			var variableHeader = this.GetVariableHeader (message);
			var payload = this.GetPayload (message);
			var remainingLength = Protocol.Encoding.EncodeRemainingLength (variableHeader.Length + payload.Length);
			var fixedHeader = this.GetFixedHeader (remainingLength);

			packet.AddRange (fixedHeader);
			packet.AddRange (variableHeader);
			packet.AddRange (payload);

			return packet.ToArray();
		}

		private byte[] GetFixedHeader(byte[] remainingLength)
		{
			var fixedHeader = new List<byte> ();

			var flags = 0x02;
			var type = Convert.ToInt32(MessageType.Unsubscribe) << 4;

			var fixedHeaderByte1 = Convert.ToByte(flags | type);

			fixedHeader.Add (fixedHeaderByte1);
			fixedHeader.AddRange (remainingLength);

			return fixedHeader.ToArray();
		}

		private byte[] GetVariableHeader(Unsubscribe message)
		{
			var variableHeader = new List<byte> ();

			var messageIdBytes = Protocol.Encoding.EncodeBigEndian(message.MessageId);

			variableHeader.AddRange (messageIdBytes);

			return variableHeader.ToArray();
		}

		private byte[] GetPayload(Unsubscribe message)
		{
			var payload = new List<byte> ();

			foreach (var topic in message.Topics) {
				var topicBytes = Protocol.Encoding.EncodeString (topic);

				payload.AddRange (topicBytes);
			}

			return payload.ToArray ();
		}
	}
}
