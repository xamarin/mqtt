using System.Net.Mqtt.Sdk.Packets;

namespace System.Net.Mqtt.Sdk.Formatters
{
	internal class ConnectAckFormatter : Formatter<ConnectAck>
	{
		public override MqttPacketType PacketType { get { return Packets.MqttPacketType.ConnectAck; } }

		protected override ConnectAck Read (byte[] bytes)
		{
			ValidateHeaderFlag (bytes, t => t == MqttPacketType.ConnectAck, 0x00);

			var remainingLengthBytesLength = 0;

			MqttProtocol.Encoding.DecodeRemainingLength (bytes, out remainingLengthBytesLength);

			var connectAckFlagsIndex = MqttProtocol.PacketTypeLength + remainingLengthBytesLength;

			if (bytes.Byte (connectAckFlagsIndex).Bits (7) != 0x00)
				throw new MqttException (Properties.Resources.ConnectAckFormatter_InvalidAckFlags);

			var sessionPresent = bytes.Byte (connectAckFlagsIndex).IsSet(0);
			var returnCode = (MqttConnectionStatus)bytes.Byte (connectAckFlagsIndex + 1);

			if (returnCode != MqttConnectionStatus.Accepted && sessionPresent)
				throw new MqttException (Properties.Resources.ConnectAckFormatter_InvalidSessionPresentForErrorReturnCode);

			var connectAck = new ConnectAck(returnCode, sessionPresent);

			return connectAck;
		}

		protected override byte[] Write (ConnectAck packet)
		{
			var variableHeader = GetVariableHeader (packet);
			var remainingLength = MqttProtocol.Encoding.EncodeRemainingLength (variableHeader.Length);
			var fixedHeader = GetFixedHeader (remainingLength);
			var bytes = new byte[fixedHeader.Length + variableHeader.Length];

			fixedHeader.CopyTo (bytes, 0);
			variableHeader.CopyTo (bytes, fixedHeader.Length);

			return bytes;
		}

		byte[] GetFixedHeader (byte[] remainingLength)
		{
			var flags = 0x00;
			var type = Convert.ToInt32(MqttPacketType.ConnectAck) << 4;
			var fixedHeaderByte1 = Convert.ToByte(flags | type);
			var fixedHeader = new byte[remainingLength.Length + 1];

			fixedHeader[0] = fixedHeaderByte1;
			remainingLength.CopyTo (fixedHeader, 1);

			return fixedHeader;
		}

		byte[] GetVariableHeader (ConnectAck packet)
		{
			if (packet.Status != MqttConnectionStatus.Accepted && packet.SessionPresent)
				throw new MqttException (Properties.Resources.ConnectAckFormatter_InvalidSessionPresentForErrorReturnCode);

			var connectAckFlagsByte = Convert.ToByte(packet.SessionPresent);
			var returnCodeByte = Convert.ToByte (packet.Status);

			return new[] { connectAckFlagsByte, returnCodeByte };
		}
	}
}
