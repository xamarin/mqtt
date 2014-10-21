using System;
using System.Collections.Generic;
using Hermes.Messages;

namespace Hermes.Formatters
{
	public class PublishReceivedFormatter : Formatter<PublishReceived>
	{
		public PublishReceivedFormatter (IChannel<IMessage> reader, IChannel<byte[]> writer)
			: base(reader, writer)
		{
		}

		protected override PublishReceived Format (byte[] packet)
		{
			var remainingLengthBytesLength = 0;
			
			ProtocolEncoding.DecodeRemainingLength (packet, out remainingLengthBytesLength);

			var packetIdentifierIndex = MQTT.PacketTypeLength + remainingLengthBytesLength;
			var packetIdentifierBytes = packet.Bytes (packetIdentifierIndex, 2);

			var publishReceived = new PublishReceived (packetIdentifierBytes.ToUInt16 ());

			return publishReceived;
		}

		protected override byte[] Format (PublishReceived message)
		{
			var packet = new List<byte> ();

			var variableHeader = this.GetVariableHeader (message);
			var remainingLength = ProtocolEncoding.EncodeRemainingLength (variableHeader.Length);
			var fixedHeader = this.GetFixedHeader (remainingLength);

			packet.AddRange (fixedHeader);
			packet.AddRange (variableHeader);

			return packet.ToArray();
		}

		private byte[] GetFixedHeader(byte[] remainingLength)
		{
			var fixedHeader = new List<byte> ();

			var flags = 0x00;
			var type = Convert.ToInt32(MessageType.PublishReceived) << 4;

			var fixedHeaderByte1 = Convert.ToByte(flags | type);

			fixedHeader.Add (fixedHeaderByte1);
			fixedHeader.AddRange (remainingLength);

			return fixedHeader.ToArray();
		}

		private byte[] GetVariableHeader(PublishReceived message)
		{
			var variableHeader = new List<byte> ();

			var messageIdBytes = ProtocolEncoding.EncodeBigEndian(message.MessageId);

			variableHeader.AddRange (messageIdBytes);

			return variableHeader.ToArray();
		}
	}
}
