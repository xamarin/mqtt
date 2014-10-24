using System;
using Hermes.Packets;
using Hermes.Properties;

namespace Hermes.Formatters
{
	public class ConnectAckFormatter : Formatter<ConnectAck>
	{
		public ConnectAckFormatter (IChannel<IPacket> reader, IChannel<byte[]> writer)
			: base(reader, writer)
		{
		}

		public override PacketType PacketType { get { return Packets.PacketType.ConnectAck; } }

		protected override ConnectAck Read (byte[] bytes)
		{
			this.ValidateHeaderFlag (bytes, t => t == PacketType.ConnectAck, 0x00);

			var remainingLengthBytesLength = 0;
			
			Protocol.Encoding.DecodeRemainingLength (bytes, out remainingLengthBytesLength);

			var connectAckFlagsIndex = Protocol.PacketTypeLength + remainingLengthBytesLength;

			if (bytes.Byte (connectAckFlagsIndex).Bits (7) != 0x00)
				throw new ProtocolException (Resources.ConnectAckFormatter_InvalidAckFlags);

			var sessionPresent = bytes.Byte (connectAckFlagsIndex).IsSet(0);
			var returnCode = (ConnectionStatus)bytes.Byte (connectAckFlagsIndex + 1);

			if (returnCode != ConnectionStatus.Accepted && sessionPresent)
				throw new ProtocolException (Resources.ConnectAckFormatter_InvalidSessionPresentForErrorReturnCode);

			var connectAck = new ConnectAck(returnCode, sessionPresent);

			return connectAck;
		}

		protected override byte[] Write (ConnectAck packet)
		{
			var variableHeader = this.GetVariableHeader (packet);
			var remainingLength = Protocol.Encoding.EncodeRemainingLength (variableHeader.Length);
			var fixedHeader = this.GetFixedHeader (remainingLength);
			var bytes = new byte[fixedHeader.Length + variableHeader.Length];

			fixedHeader.CopyTo(bytes, 0);
			variableHeader.CopyTo(bytes, fixedHeader.Length);

			return bytes;
		}

		private byte[] GetFixedHeader(byte[] remainingLength)
		{
			var flags = 0x00;
			var type = Convert.ToInt32(PacketType.ConnectAck) << 4;
			var fixedHeaderByte1 = Convert.ToByte(flags | type);
			var fixedHeader = new byte[remainingLength.Length + 1];

			fixedHeader[0] = fixedHeaderByte1;
			remainingLength.CopyTo(fixedHeader, 1);

			return fixedHeader;
		}

		private byte[] GetVariableHeader(ConnectAck packet)
		{
			if (packet.Status != ConnectionStatus.Accepted && packet.ExistingSession)
				throw new ProtocolException (Resources.ConnectAckFormatter_InvalidSessionPresentForErrorReturnCode);

			var connectAckFlagsByte = Convert.ToByte(packet.ExistingSession);
			var returnCodeByte = Convert.ToByte (packet.Status);

			return new[] { connectAckFlagsByte, returnCodeByte };
		}
	}
}
