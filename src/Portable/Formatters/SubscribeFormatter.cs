using System;
using System.Collections.Generic;
using Hermes.Messages;

namespace Hermes.Formatters
{
	public class SubscribeFormatter : Formatter<Subscribe>
	{
		public SubscribeFormatter (IChannel<IMessage> reader, IChannel<byte[]> writer)
			: base(reader, writer)
		{
		}

		protected override Subscribe Format (byte[] packet)
		{
			var remainingLengthBytesLength = 0;
			var remainingLength = ProtocolEncoding.DecodeRemainingLength (packet, out remainingLengthBytesLength);

			var packetIdentifierStartIndex = remainingLengthBytesLength + 1;
			var packetIdentifier = packet.Bytes (packetIdentifierStartIndex, 2).ToUInt16();

			var headerLength = 1 + remainingLengthBytesLength + 2;
			var subscriptions = this.GetSubscriptions(packet, headerLength, remainingLength);

			return new Subscribe (packetIdentifier, subscriptions);
		}

		protected override byte[] Format (Subscribe message)
		{
			var packet = new List<byte> ();

			var variableHeader = this.GetVariableHeader (message);
			var payload = this.GetPayload (message);
			var remainingLength = ProtocolEncoding.EncodeRemainingLength (variableHeader.Length + payload.Length);
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
			var type = Convert.ToInt32(MessageType.Subscribe) << 4;

			var fixedHeaderByte1 = Convert.ToByte(flags | type);

			fixedHeader.Add (fixedHeaderByte1);
			fixedHeader.AddRange (remainingLength);

			return fixedHeader.ToArray();
		}

		private byte[] GetVariableHeader(Subscribe message)
		{
			var variableHeader = new List<byte> ();

			var messageIdBytes = ProtocolEncoding.EncodeBigEndian(message.MessageId);

			variableHeader.AddRange (messageIdBytes);

			return variableHeader.ToArray();
		}

		private byte[] GetPayload(Subscribe message)
		{
			var payload = new List<byte> ();

			foreach (var subscription in message.Subscriptions) {
				var topicBytes = ProtocolEncoding.EncodeString (subscription.Topic);
				var requestedQosByte = Convert.ToByte (subscription.RequestedQualityOfService);

				payload.AddRange (topicBytes);
				payload.Add (requestedQosByte);
			}

			return payload.ToArray ();
		}

		private Subscription[] GetSubscriptions(byte[] packet, int headerLength, int remainingLength)
		{
			var subscriptions = new List<Subscription> ();
			var index = headerLength;

			//The packet is iterated until the last byte, knowing that a valid string is always preceded by two aditional bytes (string length)
			//TODO: Analyze wether we should throw an exception when the last bytes of the packet are not part of a valid string
			do {
				var topic = packet.GetString (index, out index);
				var requestedQos = (QualityOfService)packet.Byte (index).Bits(7, 2);

				subscriptions.Add(new Subscription(topic, requestedQos));
				index++;
			} while (packet.Length - index + 1 >= 2);

			return subscriptions.ToArray();
		}
	}
}
