using System.Collections.Generic;
using System.Net.Mqtt.Sdk.Packets;
using System.Text.RegularExpressions;

namespace System.Net.Mqtt.Sdk.Formatters
{
	internal class ConnectFormatter : Formatter<Connect>
	{
		public override MqttPacketType PacketType { get { return Packets.MqttPacketType.Connect; } }

		protected override Connect Read (byte[] bytes)
		{
			ValidateHeaderFlag (bytes, t => t == MqttPacketType.Connect, 0x00);

			var remainingLengthBytesLength = 0;

			MqttProtocol.Encoding.DecodeRemainingLength (bytes, out remainingLengthBytesLength);

			var protocolName = bytes.GetString (MqttProtocol.PacketTypeLength + remainingLengthBytesLength);

			if (protocolName != MqttProtocol.Name) {
				var error = string.Format (Properties.Resources.ConnectFormatter_InvalidProtocolName, protocolName);

				throw new MqttException (error);
			}

			var protocolLevelIndex = MqttProtocol.PacketTypeLength + remainingLengthBytesLength + MqttProtocol.NameLength;
			var protocolLevel = bytes.Byte (protocolLevelIndex);

			if (protocolLevel < MqttProtocol.SupportedLevel) {
				var error = string.Format (Properties.Resources.ConnectFormatter_UnsupportedLevel, protocolLevel);

				throw new MqttConnectionException (MqttConnectionStatus.UnacceptableProtocolVersion, error);
			}

			var protocolLevelLength = 1;
			var connectFlagsIndex = protocolLevelIndex + protocolLevelLength;
			var connectFlags = bytes.Byte (connectFlagsIndex);

			if (connectFlags.IsSet (0))
				throw new MqttException (Properties.Resources.ConnectFormatter_InvalidReservedFlag);

			if (connectFlags.Bits (4, 2) == 0x03)
				throw new MqttException (Properties.Resources.Formatter_InvalidQualityOfService);

			var willFlag = connectFlags.IsSet (2);
			var willRetain = connectFlags.IsSet (5);

			if (!willFlag && willRetain)
				throw new MqttException (Properties.Resources.ConnectFormatter_InvalidWillRetainFlag);

			var userNameFlag = connectFlags.IsSet (7);
			var passwordFlag = connectFlags.IsSet (6);

			if (!userNameFlag && passwordFlag)
				throw new MqttException (Properties.Resources.ConnectFormatter_InvalidPasswordFlag);

			var willQos = (MqttQualityOfService)connectFlags.Bits (4, 2);
			var cleanSession = connectFlags.IsSet (1);

			var keepAliveLength = 2;
			var keepAliveBytes = bytes.Bytes (connectFlagsIndex + 1, keepAliveLength);
			var keepAlive = keepAliveBytes.ToUInt16 ();

			var payloadStartIndex = connectFlagsIndex + keepAliveLength + 1;
			var nextIndex = 0;
			var clientId = bytes.GetString (payloadStartIndex, out nextIndex);

			if (clientId.Length > MqttProtocol.ClientIdMaxLength)
				throw new MqttConnectionException (MqttConnectionStatus.IdentifierRejected, Properties.Resources.ConnectFormatter_ClientIdMaxLengthExceeded);

			if (!IsValidClientId (clientId)) {
				var error = string.Format (Properties.Resources.ConnectFormatter_InvalidClientIdFormat, clientId);

				throw new MqttConnectionException (MqttConnectionStatus.IdentifierRejected, error);
			}

			if (string.IsNullOrEmpty (clientId) && !cleanSession)
				throw new MqttConnectionException (MqttConnectionStatus.IdentifierRejected, Properties.Resources.ConnectFormatter_ClientIdEmptyRequiresCleanSession);

			if (string.IsNullOrEmpty (clientId)) {
				clientId = MqttClient.GetAnonymousClientId ();
			}

			var connect = new Connect (clientId, cleanSession);

			connect.KeepAlive = keepAlive;

			if (willFlag) {
				var willTopic = bytes.GetString (nextIndex, out int willMessageIndex);
				var willMessageLengthBytes = bytes.Bytes (willMessageIndex, count: 2);
				var willMessageLenght = willMessageLengthBytes.ToUInt16 ();

				var willMessage = bytes.Bytes (willMessageIndex + 2, willMessageLenght);

				connect.Will = new MqttLastWill (willTopic, willQos, willRetain, willMessage);
				nextIndex = willMessageIndex + 2 + willMessageLenght;
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

			var flags = 0x00;
			var type = Convert.ToInt32(MqttPacketType.Connect) << 4;

			var fixedHeaderByte1 = Convert.ToByte(flags | type);

			fixedHeader.Add (fixedHeaderByte1);
			fixedHeader.AddRange (remainingLength);

			return fixedHeader.ToArray ();
		}

		byte[] GetVariableHeader (Connect packet)
		{
			var variableHeader = new List<byte> ();

			var protocolNameBytes = MqttProtocol.Encoding.EncodeString(MqttProtocol.Name);
			var protocolLevelByte = Convert.ToByte(MqttProtocol.SupportedLevel);

			var reserved = 0x00;
			var cleanSession = Convert.ToInt32 (packet.CleanSession);
			var willFlag = Convert.ToInt32 (packet.Will != null);
			var willQos = packet.Will == null ? 0 : Convert.ToInt32(packet.Will.QualityOfService);
			var willRetain = packet.Will == null ? 0 : Convert.ToInt32(packet.Will.Retain);
			var userNameFlag = Convert.ToInt32 (!string.IsNullOrEmpty (packet.UserName));
			var passwordFlag = userNameFlag == 1 ? Convert.ToInt32 (!string.IsNullOrEmpty (packet.Password)) : 0;

			if (userNameFlag == 0 && passwordFlag == 1)
				throw new MqttException (Properties.Resources.ConnectFormatter_InvalidPasswordFlag);

			cleanSession <<= 1;
			willFlag <<= 2;
			willQos <<= 3;
			willRetain <<= 5;
			passwordFlag <<= 6;
			userNameFlag <<= 7;

			var connectFlagsByte = Convert.ToByte(reserved | cleanSession | willFlag | willQos | willRetain | passwordFlag | userNameFlag);
			var keepAliveBytes = MqttProtocol.Encoding.EncodeInteger(packet.KeepAlive);

			variableHeader.AddRange (protocolNameBytes);
			variableHeader.Add (protocolLevelByte);
			variableHeader.Add (connectFlagsByte);
			variableHeader.Add (keepAliveBytes[keepAliveBytes.Length - 2]);
			variableHeader.Add (keepAliveBytes[keepAliveBytes.Length - 1]);

			return variableHeader.ToArray ();
		}

		byte[] GetPayload (Connect packet)
		{
			if (packet.ClientId.Length > MqttProtocol.ClientIdMaxLength)
				throw new MqttException (Properties.Resources.ConnectFormatter_ClientIdMaxLengthExceeded);

			if (!IsValidClientId (packet.ClientId)) {
				var error = string.Format (Properties.Resources.ConnectFormatter_InvalidClientIdFormat, packet.ClientId);

				throw new MqttException (error);
			}

			var payload = new List<byte> ();

			var clientIdBytes = MqttProtocol.Encoding.EncodeString (packet.ClientId);

			payload.AddRange (clientIdBytes);

			if (packet.Will != null) {
				var willTopicBytes = MqttProtocol.Encoding.EncodeString (packet.Will.Topic);
				var willMessageBytes = packet.Will.Payload;
				var willMessageLengthBytes = MqttProtocol.Encoding.EncodeInteger (willMessageBytes.Length);

				payload.AddRange (willTopicBytes);
				payload.Add (willMessageLengthBytes [willMessageLengthBytes.Length - 2]);
				payload.Add (willMessageLengthBytes [willMessageLengthBytes.Length - 1]);
				payload.AddRange (willMessageBytes);
			}

			if (string.IsNullOrEmpty (packet.UserName) && !string.IsNullOrEmpty (packet.Password))
				throw new MqttException (Properties.Resources.ConnectFormatter_PasswordNotAllowed);

			if (!string.IsNullOrEmpty (packet.UserName)) {
				var userNameBytes = MqttProtocol.Encoding.EncodeString(packet.UserName);

				payload.AddRange (userNameBytes);
			}

			if (!string.IsNullOrEmpty (packet.Password)) {
				var passwordBytes = MqttProtocol.Encoding.EncodeString(packet.Password);

				payload.AddRange (passwordBytes);
			}

			return payload.ToArray ();
		}

		bool IsValidClientId (string clientId)
		{
			if (string.IsNullOrEmpty (clientId))
				return true;

			var regex = new Regex ("^[a-zA-Z0-9_@]+$");

			return regex.IsMatch (clientId);
		}
	}
}
