using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Hermes.Packets;
using Hermes.Properties;

namespace Hermes.Formatters
{
	public class ConnectFormatter : Formatter<Connect>
	{
		public ConnectFormatter (IChannel<IPacket> reader, IChannel<byte[]> writer)
			: base(reader, writer)
		{
		}

		public override PacketType PacketType { get { return Packets.PacketType.Connect; } }

		protected override Connect Read (byte[] bytes)
		{
			this.ValidateHeaderFlag (bytes, t => t == PacketType.Connect, 0x00);

			var remainingLengthBytesLength = 0;
			
			Protocol.Encoding.DecodeRemainingLength (bytes, out remainingLengthBytesLength);

			var protocolName = bytes.GetString (Protocol.PacketTypeLength + remainingLengthBytesLength);

			if (protocolName != Protocol.Name) {
				var error = string.Format(Resources.ConnectFormatter_InvalidProtocolName, protocolName);

				throw new ProtocolException (error);
			}

			var protocolLevelLength = 1;
			var connectFlagsIndex = Protocol.PacketTypeLength + remainingLengthBytesLength + Protocol.NameLength + protocolLevelLength;
			var connectFlags = bytes.Byte (connectFlagsIndex);

			if (connectFlags.IsSet (0))
				throw new ProtocolException (Resources.ConnectFormatter_InvalidReservedFlag);

			if (connectFlags.Bits (4, 2) == 0x03)
				throw new ProtocolException (Resources.Formatter_InvalidQualityOfService);

			var willFlag = connectFlags.IsSet (2);
			var willRetain = connectFlags.IsSet (5);

			if (!willFlag && willRetain)
				throw new ProtocolException (Resources.ConnectFormatter_InvalidWillRetainFlag);

			var userNameFlag = connectFlags.IsSet (7);
			var passwordFlag = connectFlags.IsSet (6);
			
			if (!userNameFlag && passwordFlag)
				throw new ProtocolException (Resources.ConnectFormatter_InvalidPasswordFlag);

			var willQos = (QualityOfService)connectFlags.Bits (4, 2);
			var cleanSession = connectFlags.IsSet (1);

			var keepAliveLength = 2;
			var keepAliveBytes = bytes.Bytes(connectFlagsIndex + 1, keepAliveLength);
			var keepAlive = keepAliveBytes.ToUInt16 ();

			var payloadStartIndex = connectFlagsIndex + keepAliveLength + 1;
			var nextIndex = 0;
			var clientId = bytes.GetString (payloadStartIndex, out nextIndex);

			if (string.IsNullOrEmpty (clientId))
				throw new ConnectProtocolException (ConnectionStatus.IdentifierRejected, Resources.ConnectFormatter_ClientIdRequired);

			if (clientId.Length > Protocol.ClientIdMaxLength)
				throw new ConnectProtocolException (ConnectionStatus.IdentifierRejected, Resources.ConnectFormatter_ClientIdMaxLengthExceeded);

			if (!this.IsValidClientId (clientId)) {
				var error = string.Format (Resources.ConnectFormatter_InvalidClientIdFormat, clientId);

				throw new ConnectProtocolException (ConnectionStatus.IdentifierRejected, error);
			}

			var connect = new Connect (clientId, cleanSession);

			connect.KeepAlive = keepAlive;

			if (willFlag) {
				var willMessageIndex = 0;
				var willTopic = bytes.GetString (nextIndex, out willMessageIndex);
				var willMessage = bytes.GetString (willMessageIndex, out nextIndex);

				connect.Will = new Will (willTopic, willQos, willRetain, willMessage);
			}

			if (userNameFlag) {
				var userName = bytes.GetString (nextIndex, out nextIndex);

				connect.UserName = userName;
			}

			if (passwordFlag) {
				var password = bytes.GetString (nextIndex);

				connect.Password = password;
			}

			return connect;
		}

		protected override byte[] Write (Connect packet)
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

			var flags = 0x00;
			var type = Convert.ToInt32(PacketType.Connect) << 4;

			var fixedHeaderByte1 = Convert.ToByte(flags | type);

			fixedHeader.Add (fixedHeaderByte1);
			fixedHeader.AddRange (remainingLength);

			return fixedHeader.ToArray();
		}

		private byte[] GetVariableHeader(Connect packet)
		{
			var variableHeader = new List<byte> ();

			var protocolNameBytes = Protocol.Encoding.EncodeString(Protocol.Name);
			var protocolLevelByte = Convert.ToByte(Protocol.Level);

			var reserved = 0x00;
			var cleanSession = Convert.ToInt32 (packet.CleanSession);
			var willFlag = Convert.ToInt32 (packet.Will != null);
			var willQos = packet.Will == null ? 0 : Convert.ToInt32(packet.Will.QualityOfService);
			var willRetain = packet.Will == null ? 0 : Convert.ToInt32(packet.Will.Retain);
			var userNameFlag = Convert.ToInt32 (!string.IsNullOrEmpty (packet.UserName));
			var passwordFlag = userNameFlag == 1 ? Convert.ToInt32 (!string.IsNullOrEmpty (packet.Password)) : 0;

			if (userNameFlag == 0 && passwordFlag == 1)
				throw new ProtocolException (Resources.ConnectFormatter_InvalidPasswordFlag);

			cleanSession <<= 1;
			willFlag <<= 2;
			willQos <<= 3;
			willRetain <<= 5;
			passwordFlag <<= 6;
			userNameFlag <<= 7;

			var connectFlagsByte = Convert.ToByte(reserved | cleanSession | willFlag | willQos | willRetain | passwordFlag | userNameFlag);
			var keepAliveBytes = Protocol.Encoding.EncodeBigEndian(packet.KeepAlive);

			variableHeader.AddRange (protocolNameBytes);
			variableHeader.Add (protocolLevelByte);
			variableHeader.Add (connectFlagsByte);
			variableHeader.Add (keepAliveBytes[keepAliveBytes.Length - 2]);
			variableHeader.Add (keepAliveBytes[keepAliveBytes.Length - 1]);

			return variableHeader.ToArray();
		}

		private byte[] GetPayload(Connect packet)
		{
			if (string.IsNullOrEmpty (packet.ClientId))
				throw new ProtocolException (Resources.ConnectFormatter_ClientIdRequired);

			if (packet.ClientId.Length > Protocol.ClientIdMaxLength)
				throw new ProtocolException (Resources.ConnectFormatter_ClientIdMaxLengthExceeded);

			if (!this.IsValidClientId (packet.ClientId)) {
				var error = string.Format (Resources.ConnectFormatter_InvalidClientIdFormat, packet.ClientId);

				throw new ProtocolException (error);
			}

			var payload = new List<byte> ();

			var clientIdBytes = Protocol.Encoding.EncodeString(packet.ClientId);

			payload.AddRange(clientIdBytes);

			if (packet.Will != null) {
				var willTopicBytes = Protocol.Encoding.EncodeString(packet.Will.Topic);
				var willMessageBytes = Protocol.Encoding.EncodeString(packet.Will.Message);

				payload.AddRange (willTopicBytes);
				payload.AddRange (willMessageBytes);
			}

			if (string.IsNullOrEmpty (packet.UserName) && !string.IsNullOrEmpty (packet.Password))
				throw new ProtocolException (Resources.ConnectFormatter_PasswordNotAllowed);

			if (!string.IsNullOrEmpty (packet.UserName)) {
				var userNameBytes = Protocol.Encoding.EncodeString(packet.UserName);

				payload.AddRange (userNameBytes);
			}

			if (!string.IsNullOrEmpty (packet.Password)) {
				var passwordBytes = Protocol.Encoding.EncodeString(packet.Password);

				payload.AddRange (passwordBytes);
			}

			return payload.ToArray ();
		}

		private bool IsValidClientId(string clientId)
		{
			var regex = new Regex ("^[a-zA-Z0-9]+$");

			return regex.IsMatch (clientId);
		}
	}
}
