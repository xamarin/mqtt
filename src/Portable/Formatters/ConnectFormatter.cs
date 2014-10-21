using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Hermes.Messages;
using Hermes.Properties;

namespace Hermes.Formatters
{
	public class ConnectFormatter : Formatter<Connect>
	{
		public ConnectFormatter (IChannel<IMessage> reader, IChannel<byte[]> writer)
			: base(reader, writer)
		{
		}

		protected override Connect Format (byte[] packet)
		{
			var remainingLengthBytesLength = 0;
			
			ProtocolEncoding.DecodeRemainingLength (packet, out remainingLengthBytesLength);

			var protocolLevelLength = 1;
			var connectFlagsIndex = MQTT.PacketTypeLength + remainingLengthBytesLength + MQTT.NameLength + protocolLevelLength;
			var connectFlags = packet.Byte (connectFlagsIndex);

			var userNameFlag = connectFlags.IsSet (7);
			var passwordFlag = connectFlags.IsSet (6);
			var willRetain = connectFlags.IsSet (5);
			var willQos = (QualityOfService)connectFlags.Bits (4, 2);
			var willFlag = connectFlags.IsSet (2);
			var cleanSession = connectFlags.IsSet (1);

			var keepAliveLength = 2;
			var keepAliveBytes = packet.Bytes(connectFlagsIndex + 1, keepAliveLength);
			var keepAlive = keepAliveBytes.ToUInt16 ();

			var payloadStartIndex = connectFlagsIndex + keepAliveLength + 1;
			var nextIndex = 0;
			var clientId = packet.GetString (payloadStartIndex, out nextIndex);

			var connect = new Connect (clientId, cleanSession);

			connect.KeepAlive = keepAlive;

			if (willFlag) {
				var willMessageIndex = 0;
				var willTopic = packet.GetString (nextIndex, out willMessageIndex);
				var willMessage = packet.GetString (willMessageIndex, out nextIndex);

				connect.Will = new Will (willTopic, willQos, willRetain, willMessage);
			}

			if (userNameFlag) {
				var userName = packet.GetString (nextIndex, out nextIndex);

				connect.UserName = userName;
			}

			if (passwordFlag) {
				var password = packet.GetString (nextIndex);

				connect.Password = password;
			}

			return connect;
		}

		protected override byte[] Format (Connect message)
		{
			var packet = new List<byte> ();

			var variableHeader = this.GetVariableHeader (message);
			var payload = this.GetPayload (message);
			var remainingLength = ProtocolEncoding.EncodeRemainingLength (variableHeader.Length + payload.Length);
			var fixedHeader = this.GetFixedHeader (remainingLength);

			packet.AddRange (fixedHeader);
			packet.AddRange (variableHeader);
			packet.AddRange (payload);

			return packet.ToArray();
		}

		private byte[] GetFixedHeader(byte[] remainingLength)
		{
			var fixedHeader = new List<byte> ();

			var flags = 0x00;
			var type = Convert.ToInt32(MessageType.Connect) << 4;

			var fixedHeaderByte1 = Convert.ToByte(flags | type);

			fixedHeader.Add (fixedHeaderByte1);
			fixedHeader.AddRange (remainingLength);

			return fixedHeader.ToArray();
		}

		private byte[] GetVariableHeader(Connect message)
		{
			var variableHeader = new List<byte> ();

			var protocolNameBytes = ProtocolEncoding.EncodeString(MQTT.Name);
			var protocolLevelByte = Convert.ToByte(MQTT.ProtocolLevel);

			var reserved = 0x00;
			var cleanSession = Convert.ToInt32 (message.CleanSession);
			var willFlag = Convert.ToInt32 (message.Will != null);
			var willQos = message.Will == null ? 0 : Convert.ToInt32(message.Will.QualityOfService);
			var willRetain = message.Will == null ? 0 : Convert.ToInt32(message.Will.Retain);
			var userNameFlag = Convert.ToInt32 (!string.IsNullOrEmpty (message.UserName));
			var passwordFlag = userNameFlag == 1 ? Convert.ToInt32 (!string.IsNullOrEmpty (message.Password)) : 0;

			cleanSession <<= 1;
			willFlag <<= 2;
			willQos <<= 3;
			willRetain <<= 5;
			passwordFlag <<= 6;
			userNameFlag <<= 7;

			var connectFlagsByte = Convert.ToByte(reserved | cleanSession | willFlag | willQos | willRetain | passwordFlag | userNameFlag);
			var keepAliveBytes = ProtocolEncoding.EncodeBigEndian(message.KeepAlive);

			variableHeader.AddRange (protocolNameBytes);
			variableHeader.Add (protocolLevelByte);
			variableHeader.Add (connectFlagsByte);
			variableHeader.Add (keepAliveBytes[keepAliveBytes.Length - 2]);
			variableHeader.Add (keepAliveBytes[keepAliveBytes.Length - 1]);

			return variableHeader.ToArray();
		}

		private byte[] GetPayload(Connect message)
		{
			if (string.IsNullOrEmpty (message.ClientId)) {
				throw new ProtocolException (Resources.ConnectFormatter_ClientIdRequired);
			}

			if (!this.IsValidClientId (message.ClientId)) {
				throw new ProtocolException (Resources.ConnectFormatter_ClientIdInvalid);
			}

			var payload = new List<byte> ();

			var clientIdBytes = ProtocolEncoding.EncodeString(message.ClientId);

			if (clientIdBytes.Length > 23) {
				throw new ProtocolException (Resources.ConnectFormatter_ClientIdMaxLengthExceeded);
			}

			payload.AddRange(clientIdBytes);

			if (message.Will != null) {
				var willTopicBytes = ProtocolEncoding.EncodeString(message.Will.Topic);
				var willMessageBytes = ProtocolEncoding.EncodeString(message.Will.Message);

				payload.AddRange (willTopicBytes);
				payload.AddRange (willMessageBytes);
			}

			if (!string.IsNullOrEmpty (message.UserName)) {
				var userNameBytes = ProtocolEncoding.EncodeString(message.UserName);

				payload.AddRange (userNameBytes);

				if (!string.IsNullOrEmpty (message.Password)) {
					var passwordBytes = ProtocolEncoding.EncodeString(message.Password);

					payload.AddRange (passwordBytes);
				}
			}

			return payload.ToArray ();
		}

		private bool IsValidClientId(string clientId)
		{
			var regex = new Regex ("[a-zA-Z0-9]");

			return regex.IsMatch (clientId);
		}
	}
}
