using System.Collections.Generic;
using System.Net.Mqtt.Exceptions;
using System.Text;

namespace System.Net.Mqtt
{
    /// <summary>
    /// Represents an encoder and decoder of values according to the protocol specification
    /// </summary>
	public class MqttEncoder
	{
        /// <summary>
        /// Encodes a string according to the MQTT definition for strings,
        /// which involves a 2 byte length field to determine the number of bytes in the string itself
        /// </summary>
        /// <remarks>
        /// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/errata01/os/mqtt-v3.1.1-errata01-os-complete.html#_Toc442180829">UTF-8 encoded strings</a>
        /// for more details about the MQTT specification for strings
        /// </remarks>
        /// <param name="text">Text to encode</param>
        /// <returns>The encoded string as a byte[]</returns>
        /// <exception cref="MqttException">MqttException</exception>
        public byte[] EncodeString (string text)
		{
			if (string.IsNullOrEmpty (text)) {
				return new byte[] { };
			}

			var bytes = new List<byte> ();
			var textBytes = Encoding.UTF8.GetBytes (text);

			if (textBytes.Length > MqttProtocol.MaxIntegerLength) {
				throw new MqttException  (Properties.Resources.ProtocolEncoding_StringMaxLengthExceeded);
			}

			var numberBytes = MqttProtocol.Encoding.EncodeInteger (textBytes.Length);

			bytes.Add (numberBytes[numberBytes.Length - 2]);
			bytes.Add (numberBytes[numberBytes.Length - 1]);
			bytes.AddRange (textBytes);

			return bytes.ToArray ();
		}

        /// <summary>
        /// Encodes an Int32 according to the MQTT specification,
        /// which considers the integers in big-endian order, meaning the high order byte
        /// precedes the lower order byte
        /// </summary>
        /// <remarks>
        /// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/errata01/os/mqtt-v3.1.1-errata01-os-complete.html#_Toc442180828">Integer data values</a>
        /// for more details about the MQTT specification for integers
        /// </remarks>
        /// <param name="number">Int32 to encode</param>
        /// <returns>The encoded Int32 as a byte[]</returns>
        /// <exception cref="MqttException">MqttException</exception>
        public byte[] EncodeInteger (int number)
		{
			if (number > MqttProtocol.MaxIntegerLength) {
				throw new MqttException  (Properties.Resources.ProtocolEncoding_IntegerMaxValueExceeded);
			}

			return EncodeInteger ((ushort)number);
		}

        /// <summary>
        /// Encodes an Int16 according to the MQTT specification,
        /// which considers the integers in big-endian order, meaning the high order byte
        /// precedes the lower order byte
        /// </summary>
        /// <remarks>
        /// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/errata01/os/mqtt-v3.1.1-errata01-os-complete.html#_Toc442180828">Integer data values</a>
        /// for more details about the MQTT specification for integers
        /// </remarks>
        /// <param name="number">Int16 to encode</param>
        /// <returns>The encoded Int16 as a byte[]</returns>
        /// <exception cref="MqttException">MqttException</exception>
        public byte[] EncodeInteger (ushort number)
		{
			var bytes = BitConverter.GetBytes (number);

			if (BitConverter.IsLittleEndian) {
				Array.Reverse (bytes);
			}

			return bytes;
		}

        /// <summary>
        /// Encodes the packet remaining length, which means
        /// the number of bytes remaining within the current packet, 
        /// including data in the variable header and the payload
        /// The encoding is done following the algorithm suggested in the specification
        /// <remarks>
        /// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/errata01/os/mqtt-v3.1.1-errata01-os-complete.html#_Toc442180836">Remaining length</a>
        /// for more details about remaining length encoding and decoding
        /// </remarks>
        /// </summary>
        /// <param name="length">The remaining lenght to encode, as a number</param>
        /// <returns>The encoded remaining length as a byte[]</returns>
		public byte[] EncodeRemainingLength (int length)
		{
			var bytes = new List<byte> ();
			var encoded = default(int);

			do {
				encoded = length % 128;
				length = length / 128;

				if (length > 0) {
					encoded = encoded | 128;
				}

				bytes.Add (Convert.ToByte (encoded));
			} while (length > 0);

			return bytes.ToArray ();
		}

        /// <summary>
        /// Decodes the packet remaining length, which means
        /// the number of bytes remaining within the current packet, 
        /// including data in the variable header and the payload
        /// The decoding is done following the algorithm suggested in the specification
        /// <remarks>
        /// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/errata01/os/mqtt-v3.1.1-errata01-os-complete.html#_Toc442180836">Remaining length</a>
        /// for more details about remaining length encoding and decoding
        /// </remarks>
        /// </summary>
        /// <param name="packet">The packet as a byte[], to decode the remaining length</param>
        /// <param name="arrayLength">An out value that specifies the real lenght of the packet</param>
        /// <returns>The decoded remaining length as a number</returns>
        /// <exception cref="MqttException">MqttException</exception>
        public int DecodeRemainingLength (byte[] packet, out int arrayLength)
		{
			var multiplier = 1;
			var value = 0;
			var index = 0;
			var encodedByte = default(byte);

			do {
				index++;
				encodedByte = packet[index];
				value += (encodedByte & 127) * multiplier;

				if (multiplier > 128 * 128 * 128 || index > 4)
					throw new MqttException  (Properties.Resources.ProtocolEncoding_MalformedRemainingLength);

                multiplier *= 128;
            } while ((encodedByte & 128) != 0);

			arrayLength = index;

			return value;
		}
	}
}
