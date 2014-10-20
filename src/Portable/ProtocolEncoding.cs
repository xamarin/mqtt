using System;
using System.Collections.Generic;
using System.Text;
using Hermes.Properties;

namespace Hermes
{
	public static class ProtocolEncoding
	{
		/// <exception cref="ProtocolException">ProtocolException</exception>
		public static byte[] EncodeString (string text)
		{
			if (string.IsNullOrEmpty (text)) {
				return new byte[] { };
			}

			var bytes = new List<byte> ();
			var textBytes = Encoding.UTF8.GetBytes (text);

			if(textBytes.Length > 65536) {
				throw new ProtocolException(Resources.DataRepresentationExtensions_StringMaxLengthExceeded);
			}

			var numberBytes = ProtocolEncoding.EncodeBigEndian (textBytes.Length);

			bytes.Add (numberBytes[numberBytes.Length - 2]);
			bytes.Add (numberBytes[numberBytes.Length - 1]);
			bytes.AddRange (textBytes);

			return bytes.ToArray();
		}

		public static byte[] EncodeBigEndian(int number)
		{
			var bytes = BitConverter.GetBytes (number);

			if (BitConverter.IsLittleEndian) {
				Array.Reverse (bytes);
			}

			return bytes;
		}

		public static byte[] EncodeRemainingLength(int length)
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

			return bytes.ToArray();
		}

		/// <exception cref="ProtocolException">ProtocolException</exception>
		public static int DecodeRemainingLength(byte[] packet, out int arrayLength)
		{
			var multiplier = 1;
			var value = 0;
			var index = 0;
			var encodedByte = default(byte);

			do {
				index++;
				encodedByte = packet[index];
				value += (encodedByte & 127) * multiplier;
				multiplier *= 128;

				if (multiplier > 128 * 128 * 128 * 128 || index > 4)
					throw new ProtocolException (Resources.ProtocolEncoding_MalformedRemainingLength);
			} while((encodedByte & 128) != 0);

			arrayLength = index;

			return value;
		}
	}
}
