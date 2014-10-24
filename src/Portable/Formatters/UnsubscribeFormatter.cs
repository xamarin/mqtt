using System;
using System.Collections.Generic;
using System.Linq;
using Hermes.Packets;
using Hermes.Properties;

namespace Hermes.Formatters
{
	public class UnsubscribeFormatter : Formatter<Unsubscribe>
	{
		public UnsubscribeFormatter (IChannel<IPacket> reader, IChannel<byte[]> writer)
			: base(reader, writer)
		{
		}

		public override PacketType PacketType { get { return Packets.PacketType.Unsubscribe; } }

		protected override Unsubscribe Read (byte[] bytes)
		{
			this.ValidateHeaderFlag (bytes, t => t == PacketType.Unsubscribe, 0x02);

			var remainingLengthBytesLength = 0;
			var remainingLength = Protocol.Encoding.DecodeRemainingLength (bytes, out remainingLengthBytesLength);

			var packetIdentifierStartIndex = remainingLengthBytesLength + 1;
			var packetIdentifier = bytes.Bytes (packetIdentifierStartIndex, 2).ToUInt16();

			var index = 1 + remainingLengthBytesLength + 2;

			if (bytes.Length == index)
				throw new ViolationProtocolException (Resources.UnsubscribeFormatter_MissingTopics);

			var topics = new List<string> ();

			do {
				var topic = bytes.GetString (index, out index);

				topics.Add (topic);
			} while (bytes.Length - index + 1 >= 2);

			return new Unsubscribe (packetIdentifier, topics.ToArray());
		}

		protected override byte[] Write (Unsubscribe packet)
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
			var type = Convert.ToInt32(PacketType.Unsubscribe) << 4;

			var fixedHeaderByte1 = Convert.ToByte(flags | type);

			fixedHeader.Add (fixedHeaderByte1);
			fixedHeader.AddRange (remainingLength);

			return fixedHeader.ToArray();
		}

		private byte[] GetVariableHeader(Unsubscribe packet)
		{
			var variableHeader = new List<byte> ();

			var packetIdBytes = Protocol.Encoding.EncodeBigEndian(packet.PacketId);

			variableHeader.AddRange (packetIdBytes);

			return variableHeader.ToArray();
		}

		private byte[] GetPayload(Unsubscribe packet)
		{
			if(packet.Topics == null || !packet.Topics.Any())
				throw new ViolationProtocolException (Resources.UnsubscribeFormatter_MissingTopics);

			var payload = new List<byte> ();

			foreach (var topic in packet.Topics) {
				var topicBytes = Protocol.Encoding.EncodeString (topic);

				payload.AddRange (topicBytes);
			}

			return payload.ToArray ();
		}
	}
}
