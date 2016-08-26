using System.Collections.Generic;
using System.Linq;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Exceptions;

namespace System.Net.Mqtt.Formatters
{
	internal class SubscribeAckFormatter : Formatter<SubscribeAck>
	{
		public override PacketType PacketType { get { return Packets.PacketType.SubscribeAck; } }

		protected override SubscribeAck Read (byte[] bytes)
		{
			ValidateHeaderFlag (bytes, t => t == PacketType.SubscribeAck, 0x00);

			var remainingLengthBytesLength = 0;
			var remainingLength = Protocol.Encoding.DecodeRemainingLength (bytes, out remainingLengthBytesLength);

			var packetIdentifierStartIndex = remainingLengthBytesLength + 1;
			var packetIdentifier = bytes.Bytes (packetIdentifierStartIndex, 2).ToUInt16();

			var headerLength = 1 + remainingLengthBytesLength + 2;
			var returnCodeBytes = bytes.Bytes(headerLength);

			if (!returnCodeBytes.Any ())
				throw new MqttViolationException (Properties.Resources.SubscribeAckFormatter_MissingReturnCodes);

			if (returnCodeBytes.Any (b => !Enum.IsDefined (typeof (SubscribeReturnCode), b)))
				throw new MqttViolationException (Properties.Resources.SubscribeAckFormatter_InvalidReturnCodes);

			var returnCodes = returnCodeBytes.Select(b => (SubscribeReturnCode)b).ToArray();

			return new SubscribeAck (packetIdentifier, returnCodes);
		}

		protected override byte[] Write (SubscribeAck packet)
		{
			var bytes = new List<byte> ();

			var variableHeader = GetVariableHeader (packet);
			var payload = GetPayload (packet);
			var remainingLength = Protocol.Encoding.EncodeRemainingLength (variableHeader.Length + payload.Length);
			var fixedHeader = GetFixedHeader (remainingLength);

			bytes.AddRange (fixedHeader);
			bytes.AddRange (variableHeader);
			bytes.AddRange (payload);

			return bytes.ToArray ();
		}

		byte[] GetFixedHeader (byte[] remainingLength)
		{
			var fixedHeader = new List<byte> ();

			var flags = 0x00;
			var type = Convert.ToInt32(PacketType.SubscribeAck) << 4;

			var fixedHeaderByte1 = Convert.ToByte(flags | type);

			fixedHeader.Add (fixedHeaderByte1);
			fixedHeader.AddRange (remainingLength);

			return fixedHeader.ToArray ();
		}

		byte[] GetVariableHeader (SubscribeAck packet)
		{
			var variableHeader = new List<byte> ();

			var packetIdBytes = Protocol.Encoding.EncodeInteger(packet.PacketId);

			variableHeader.AddRange (packetIdBytes);

			return variableHeader.ToArray ();
		}

		byte[] GetPayload (SubscribeAck packet)
		{
			if (packet.ReturnCodes == null || !packet.ReturnCodes.Any ())
				throw new MqttViolationException (Properties.Resources.SubscribeAckFormatter_MissingReturnCodes);

			return packet.ReturnCodes
				.Select (c => Convert.ToByte (c))
				.ToArray ();
		}
	}
}
