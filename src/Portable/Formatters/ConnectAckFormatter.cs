using System;
using System.Collections.Generic;
using Hermes.Messages;

namespace Hermes.Formatters
{
	public class ConnectAckFormatter : Formatter<ConnectAck>
	{
		public ConnectAckFormatter (IChannel<IMessage> reader, IChannel<byte[]> writer)
			: base(reader, writer)
		{
		}

		protected override ConnectAck Format (byte[] packet)
		{
			var remainingLengthBytesLength = 0;
			
			ProtocolEncoding.DecodeRemainingLength (packet, out remainingLengthBytesLength);

			var connectAckFlagsIndex = MQTT.PacketTypeLength + remainingLengthBytesLength;
			var sessionPresent = packet.Byte (connectAckFlagsIndex).IsSet(0);
			var returnCode = (ConnectionStatus)packet.Byte (connectAckFlagsIndex + 1);

			var connectAck = new ConnectAck(returnCode, sessionPresent);

			return connectAck;
		}

		protected override byte[] Format (ConnectAck message)
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
			var type = Convert.ToInt32(MessageType.ConnectAck) << 4;

			var fixedHeaderByte1 = Convert.ToByte(flags | type);

			fixedHeader.Add (fixedHeaderByte1);
			fixedHeader.AddRange (remainingLength);

			return fixedHeader.ToArray();
		}

		private byte[] GetVariableHeader(ConnectAck message)
		{
			var variableHeader = new List<byte> ();

			var connectAckFlagsByte = Convert.ToByte(message.ExistingSession);
			var returnCodeByte = Convert.ToByte (message.Status);

			variableHeader.Add (connectAckFlagsByte);
			variableHeader.Add (returnCodeByte);

			return variableHeader.ToArray();
		}
	}
}
