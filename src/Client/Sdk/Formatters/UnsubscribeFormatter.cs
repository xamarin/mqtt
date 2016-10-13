using System.Collections.Generic;
using System.Linq;
using System.Net.Mqtt.Sdk.Packets;

namespace System.Net.Mqtt.Sdk.Formatters
{
	internal class UnsubscribeFormatter : Formatter<Unsubscribe>
	{
		public override MqttPacketType PacketType { get { return Packets.MqttPacketType.Unsubscribe; } }

		protected override Unsubscribe Read (byte[] bytes)
		{
			ValidateHeaderFlag (bytes, t => t == MqttPacketType.Unsubscribe, 0x02);

			var remainingLengthBytesLength = 0;
			var remainingLength = MqttProtocol.Encoding.DecodeRemainingLength (bytes, out remainingLengthBytesLength);

			var packetIdentifierStartIndex = remainingLengthBytesLength + 1;
			var packetIdentifier = bytes.Bytes (packetIdentifierStartIndex, 2).ToUInt16();

			var index = 1 + remainingLengthBytesLength + 2;

			if (bytes.Length == index)
				throw new MqttProtocolViolationException  (Properties.Resources.UnsubscribeFormatter_MissingTopics);

			var topics = new List<string> ();

			do {
				var topic = bytes.GetString (index, out index);

				topics.Add (topic);
			} while (bytes.Length - index + 1 >= 2);

			return new Unsubscribe (packetIdentifier, topics.ToArray ());
		}

		protected override byte[] Write (Unsubscribe packet)
		{
			var bytes = new List<byte> ();

			var variableHeader = GetVariableHeader (packet);
			var payload = GetPayload (packet);
			var remainingLength = MqttProtocol.Encoding.EncodeRemainingLength (variableHeader.Length + payload.Length);
			var fixedHeader = GetFixedHeader (remainingLength);

			bytes.AddRange (fixedHeader);
			bytes.AddRange (variableHeader);
			bytes.AddRange (payload);

			return bytes.ToArray ();
		}

		byte[] GetFixedHeader (byte[] remainingLength)
		{
			var fixedHeader = new List<byte> ();

			var flags = 0x02;
			var type = Convert.ToInt32(MqttPacketType.Unsubscribe) << 4;

			var fixedHeaderByte1 = Convert.ToByte(flags | type);

			fixedHeader.Add (fixedHeaderByte1);
			fixedHeader.AddRange (remainingLength);

			return fixedHeader.ToArray ();
		}

		byte[] GetVariableHeader (Unsubscribe packet)
		{
			var variableHeader = new List<byte> ();

			var packetIdBytes = MqttProtocol.Encoding.EncodeInteger(packet.PacketId);

			variableHeader.AddRange (packetIdBytes);

			return variableHeader.ToArray ();
		}

		byte[] GetPayload (Unsubscribe packet)
		{
			if (packet.Topics == null || !packet.Topics.Any ())
				throw new MqttProtocolViolationException  (Properties.Resources.UnsubscribeFormatter_MissingTopics);

			var payload = new List<byte> ();

			foreach (var topic in packet.Topics) {
				var topicBytes = MqttProtocol.Encoding.EncodeString (topic);

				payload.AddRange (topicBytes);
			}

			return payload.ToArray ();
		}
	}
}
