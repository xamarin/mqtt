using System;
using Hermes.Messages;
using Hermes.Properties;

namespace Hermes.Formatters
{
	public class ConnectAckFormatter : Formatter<ConnectAck>
	{
		public ConnectAckFormatter (IChannel<IMessage> reader, IChannel<byte[]> writer)
			: base(reader, writer)
		{
		}

		public override MessageType MessageType { get { return Messages.MessageType.ConnectAck; } }

		protected override ConnectAck Read (byte[] packet)
		{
			this.ValidateHeaderFlag (packet, t => t == MessageType.ConnectAck, 0x00);

			var remainingLengthBytesLength = 0;
			
			Protocol.Encoding.DecodeRemainingLength (packet, out remainingLengthBytesLength);

			var connectAckFlagsIndex = Protocol.PacketTypeLength + remainingLengthBytesLength;

			if (packet.Byte (connectAckFlagsIndex).Bits (7) != 0x00)
				throw new ProtocolException (Resources.ConnectAckFormatter_InvalidAckFlags);

			var sessionPresent = packet.Byte (connectAckFlagsIndex).IsSet(0);
			var returnCode = (ConnectionStatus)packet.Byte (connectAckFlagsIndex + 1);

			if (returnCode != ConnectionStatus.Accepted && sessionPresent)
				throw new ProtocolException (Resources.ConnectAckFormatter_InvalidSessionPresentForErrorReturnCode);

			var connectAck = new ConnectAck(returnCode, sessionPresent);

			return connectAck;
		}

		protected override byte[] Write (ConnectAck message)
		{
			var variableHeader = this.GetVariableHeader (message);
			var remainingLength = Protocol.Encoding.EncodeRemainingLength (variableHeader.Length);
			var fixedHeader = this.GetFixedHeader (remainingLength);
			var packet = new byte[fixedHeader.Length + variableHeader.Length];

			fixedHeader.CopyTo(packet, 0);
			variableHeader.CopyTo(packet, fixedHeader.Length);

			return packet;
		}

		private byte[] GetFixedHeader(byte[] remainingLength)
		{
			var flags = 0x00;
			var type = Convert.ToInt32(MessageType.ConnectAck) << 4;
			var fixedHeaderByte1 = Convert.ToByte(flags | type);
			var fixedHeader = new byte[remainingLength.Length + 1];

			fixedHeader[0] = fixedHeaderByte1;
			remainingLength.CopyTo(fixedHeader, 1);

			return fixedHeader;
		}

		private byte[] GetVariableHeader(ConnectAck message)
		{
			if (message.Status != ConnectionStatus.Accepted && message.ExistingSession)
				throw new ProtocolException (Resources.ConnectAckFormatter_InvalidSessionPresentForErrorReturnCode);

			var connectAckFlagsByte = Convert.ToByte(message.ExistingSession);
			var returnCodeByte = Convert.ToByte (message.Status);

			return new[] { connectAckFlagsByte, returnCodeByte };
		}
	}
}
