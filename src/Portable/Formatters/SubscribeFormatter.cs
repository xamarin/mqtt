using System;
using System.Collections.Generic;
using System.Linq;
using Hermes.Messages;
using Hermes.Properties;

namespace Hermes.Formatters
{
	public class SubscribeFormatter : Formatter<Subscribe>
	{
		public SubscribeFormatter (IChannel<IMessage> reader, IChannel<byte[]> writer)
			: base(reader, writer)
		{
		}

		public override MessageType MessageType { get { return Messages.MessageType.Subscribe; } }

		protected override Subscribe Read (byte[] packet)
		{
			this.ValidateHeaderFlag (packet, t => t == MessageType.Subscribe, 0x02);

			var remainingLengthBytesLength = 0;
			var remainingLength = Protocol.Encoding.DecodeRemainingLength (packet, out remainingLengthBytesLength);

			var packetIdentifierStartIndex = remainingLengthBytesLength + 1;
			var packetIdentifier = packet.Bytes (packetIdentifierStartIndex, 2).ToUInt16();

			var headerLength = 1 + remainingLengthBytesLength + 2;
			var subscriptions = this.GetSubscriptions(packet, headerLength, remainingLength);

			return new Subscribe (packetIdentifier, subscriptions.ToArray());
		}

		protected override byte[] Write (Subscribe message)
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
			var type = Convert.ToInt32(MessageType.Subscribe) << 4;

			var fixedHeaderByte1 = Convert.ToByte(flags | type);

			fixedHeader.Add (fixedHeaderByte1);
			fixedHeader.AddRange (remainingLength);

			return fixedHeader.ToArray();
		}

		private byte[] GetVariableHeader(Subscribe message)
		{
			var variableHeader = new List<byte> ();

			var messageIdBytes = Protocol.Encoding.EncodeBigEndian(message.MessageId);

			variableHeader.AddRange (messageIdBytes);

			return variableHeader.ToArray();
		}

		private byte[] GetPayload(Subscribe message)
		{
			if(message.Subscriptions == null || !message.Subscriptions.Any())
				throw new ViolationProtocolException (Resources.SubscribeFormatter_MissingTopicFilterQosPair);

			var payload = new List<byte> ();

			foreach (var subscription in message.Subscriptions) {
				var topicBytes = Protocol.Encoding.EncodeString (subscription.Topic);
				var requestedQosByte = Convert.ToByte (subscription.RequestedQualityOfService);

				payload.AddRange (topicBytes);
				payload.Add (requestedQosByte);
			}

			return payload.ToArray ();
		}

		private IEnumerable<Subscription> GetSubscriptions(byte[] packet, int headerLength, int remainingLength)
		{
			if (packet.Length - headerLength < 4) //At least 4 bytes required on payload: MSB, LSB, Topic Filter, Requests QoS
				throw new ViolationProtocolException (Resources.SubscribeFormatter_MissingTopicFilterQosPair);

			var index = headerLength;

			do {
				var topic = packet.GetString (index, out index);
				var requestedQosByte = packet.Byte (index);

				if (!Enum.IsDefined (typeof (QualityOfService), requestedQosByte))
					throw new ViolationProtocolException (Resources.Formatter_InvalidQualityOfService);
	
				var requestedQos = (QualityOfService)requestedQosByte;

				yield return new Subscription(topic, requestedQos);
				index++;
			} while (packet.Length - index + 1 >= 2);
		}
	}
}
