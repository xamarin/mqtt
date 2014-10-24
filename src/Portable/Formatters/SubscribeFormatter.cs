using System;
using System.Collections.Generic;
using System.Linq;
using Hermes.Packets;
using Hermes.Properties;

namespace Hermes.Formatters
{
	public class SubscribeFormatter : Formatter<Subscribe>
	{
		public SubscribeFormatter (IChannel<IPacket> reader, IChannel<byte[]> writer)
			: base(reader, writer)
		{
		}

		public override PacketType PacketType { get { return Packets.PacketType.Subscribe; } }

		protected override Subscribe Read (byte[] bytes)
		{
			this.ValidateHeaderFlag (bytes, t => t == PacketType.Subscribe, 0x02);

			var remainingLengthBytesLength = 0;
			var remainingLength = Protocol.Encoding.DecodeRemainingLength (bytes, out remainingLengthBytesLength);

			var packetIdentifierStartIndex = remainingLengthBytesLength + 1;
			var packetIdentifier = bytes.Bytes (packetIdentifierStartIndex, 2).ToUInt16();

			var headerLength = 1 + remainingLengthBytesLength + 2;
			var subscriptions = this.GetSubscriptions(bytes, headerLength, remainingLength);

			return new Subscribe (packetIdentifier, subscriptions.ToArray());
		}

		protected override byte[] Write (Subscribe packet)
		{
			var bytes = new List<byte> ();

			var variableHeader = this.GetVariableHeader (packet);
			var payload = this.GetPayload (packet);
			var remainingLength = Protocol.Encoding.EncodeRemainingLength (variableHeader.Length + payload.Length);
			var fixedHeader = this.GetFixedHeader (remainingLength);

			bytes.AddRange (fixedHeader);
			bytes.AddRange (variableHeader);
			bytes.AddRange (payload);

			return bytes.ToArray();
		}

		private byte[] GetFixedHeader(byte[] remainingLength)
		{
			var fixedHeader = new List<byte> ();

			var flags = 0x02;
			var type = Convert.ToInt32(PacketType.Subscribe) << 4;

			var fixedHeaderByte1 = Convert.ToByte(flags | type);

			fixedHeader.Add (fixedHeaderByte1);
			fixedHeader.AddRange (remainingLength);

			return fixedHeader.ToArray();
		}

		private byte[] GetVariableHeader(Subscribe packet)
		{
			var variableHeader = new List<byte> ();

			var packetIdBytes = Protocol.Encoding.EncodeBigEndian(packet.PacketId);

			variableHeader.AddRange (packetIdBytes);

			return variableHeader.ToArray();
		}

		private byte[] GetPayload(Subscribe packet)
		{
			if(packet.Subscriptions == null || !packet.Subscriptions.Any())
				throw new ViolationProtocolException (Resources.SubscribeFormatter_MissingTopicFilterQosPair);

			var payload = new List<byte> ();

			foreach (var subscription in packet.Subscriptions) {
				var topicBytes = Protocol.Encoding.EncodeString (subscription.Topic);
				var requestedQosByte = Convert.ToByte (subscription.RequestedQualityOfService);

				payload.AddRange (topicBytes);
				payload.Add (requestedQosByte);
			}

			return payload.ToArray ();
		}

		private IEnumerable<Subscription> GetSubscriptions(byte[] bytes, int headerLength, int remainingLength)
		{
			if (bytes.Length - headerLength < 4) //At least 4 bytes required on payload: MSB, LSB, Topic Filter, Requests QoS
				throw new ViolationProtocolException (Resources.SubscribeFormatter_MissingTopicFilterQosPair);

			var index = headerLength;

			do {
				var topic = bytes.GetString (index, out index);
				var requestedQosByte = bytes.Byte (index);

				if (!Enum.IsDefined (typeof (QualityOfService), requestedQosByte))
					throw new ViolationProtocolException (Resources.Formatter_InvalidQualityOfService);
	
				var requestedQos = (QualityOfService)requestedQosByte;

				yield return new Subscription(topic, requestedQos);
				index++;
			} while (bytes.Length - index + 1 >= 2);
		}
	}
}
