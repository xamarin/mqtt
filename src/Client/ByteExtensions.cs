using System.Linq;
using System.Text;

namespace System.Net.Mqtt
{
    /// <summary>
    /// Extension methods for handling and working with bytes
    /// </summary>
	public static class ByteExtensions
	{
        /// <summary>
        /// Evaluates if a bit of a specified position in the corresponsing byte is set to 1
        /// </summary>
        /// <param name="bit">The position of the bit to evaluate, that goes from 0 to 7</param>
        /// <returns>A boolean that indicates if the bit is set to 1 or not</returns>
		public static bool IsSet (this byte @byte, int bit)
		{
			if (bit > 7)
				throw new ArgumentOutOfRangeException ("bit", Properties.Resources.ByteExtensions_InvalidBitPosition);

			return (@byte & (1 << bit)) != 0;
		}

        /// <summary>
        /// Sets a specific bit of the corresponding byte to 1
        /// </summary>
        /// <param name="bit">Specific bit to set</param>
        /// <returns>The new byte after the set of the specific bit to 1</returns>
		public static byte Set (this byte @byte, int bit)
		{
			if (bit > 7)
				throw new ArgumentOutOfRangeException ("bit", Properties.Resources.ByteExtensions_InvalidBitPosition);

			return Convert.ToByte (@byte | (1 << bit));
		}

        /// <summary>
        /// Sets a specific bit of the corresponding byte to 0
        /// </summary>
        /// <param name="bit">Specific bit to set</param>
        /// <returns>The new byte after the set of the specific bit to 0</returns>
        public static byte Unset (this byte @byte, int bit)
		{
			if (bit > 7)
				throw new ArgumentOutOfRangeException ("bit", Properties.Resources.ByteExtensions_InvalidBitPosition);

			return Convert.ToByte (@byte & ~(1 << bit));
		}

        /// <summary>
        /// Gets a portion of the byte, specified by the number of bits to strip
        /// The byte is stripped starting the count from the natural index 7
        /// The rest of the resulting byte is completed by zeros
        /// </summary>
        /// <param name="count">
        /// Number of bits to strip from the byte,
        /// starting on index 7
        /// </param>
        /// <returns>The new byte after stripping the bits out and completing the rest with zeros</returns>
		public static byte Bits (this byte @byte, int count)
		{
			return Convert.ToByte (@byte >> 8 - count);
		}

        /// <summary>
        /// Gets a portion of the byte, specified by a starting index and the number of bits to strip
        /// The starting index goes from 1 to 8, being 1 the natural index 7 of the byte
        /// The rest of the resulting byte is completed by zeros
        /// </summary>
        /// <param name="index">
        /// Starting index to strip bits. 
        /// The index goes from 1 to 8, being 1 the natural index 7 of the byte
        /// </param>
        /// <param name="count">
        /// Number of bits to strip from the byte,
        /// starting on the specified index
        /// </param>
        /// <returns>The new byte after stripping the bits out and completing the rest with zeros</returns>
		public static byte Bits (this byte @byte, int index, int count)
		{
			if (index < 1 || index > 8)
				throw new ArgumentOutOfRangeException ("index", Properties.Resources.ByteExtensions_InvalidByteIndex);

			if (index > 1) {
				var i = 1;

				do {
					@byte = @byte.Unset (8 - i);
					i++;
				} while (i < index);
			}

			var to = Convert.ToByte(@byte << index - 1);
			var from = Convert.ToByte(to >> 8 - count);

			return from;
		}

        /// <summary>
        /// Extracts a specific byte from a byte[]
        /// </summary>
        /// <param name="index">Zero based index to extract the byte from</param>
        /// <returns>The extracted byte</returns>
		public static byte Byte (this byte[] bytes, int index)
		{
			return bytes.Skip (index).Take (1).FirstOrDefault ();
		}

        /// <summary>
        /// Extracts a series of bytes from a byte[], 
        /// starting from a specified index
        /// </summary>
        /// <param name="fromIndex">Zero based index to extract the bytes from</param>
        /// <returns>The extracted bytes</returns>
        public static byte[] Bytes (this byte[] bytes, int fromIndex)
		{
			return bytes.Skip (fromIndex).ToArray ();
		}

        /// <summary>
        /// Extracts a fixed series of bytes from a byte[], 
        /// starting from a specified index and specifying the number of bytes to extract
        /// </summary>
        /// <param name="fromIndex">Zero based index to extract the bytes from</param>
        /// <param name="count">Number of bytes to extract</param>
        /// <returns>The extracted bytes</returns>
		public static byte[] Bytes (this byte[] bytes, int fromIndex, int count)
		{
			return bytes.Skip (fromIndex).Take (count).ToArray ();
		}

        /// <summary>
        /// Provides a string from a byte[], assuming that
        /// it's a UTF-8 encoded string according to the MQTT definition for strings,
        /// which involves a 2 byte length field to determine the number of bytes in the string itself
        /// </summary>
        /// <remarks>
        /// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/errata01/os/mqtt-v3.1.1-errata01-os-complete.html#_Toc442180829">UTF-8 encoded strings</a>
        /// for more details about the MQTT specification for strings
        /// </remarks>
        /// <param name="index">
        /// Zero based index from which to consider the start of the string
        /// </param>
        /// <returns>The resulting decoded string</returns>
		public static string GetString (this byte[] bytes, int index)
		{
			var length = bytes.GetStringLenght (index);

			return length == 0 ? string.Empty : Encoding.UTF8.GetString (bytes, index + MqttProtocol.StringPrefixLength, length);
		}

        /// <summary>
        /// Provides a string from a byte[], assuming that
        /// it's a UTF-8 encoded string according to the MQTT definition for strings,
        /// which involves a 2 byte length field to determine the number of bytes in the string itself
        /// </summary>
        /// <remarks>
        /// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/errata01/os/mqtt-v3.1.1-errata01-os-complete.html#_Toc442180829">UTF-8 encoded strings</a>
        /// for more details about the MQTT specification for strings
        /// </remarks>
        /// <param name="index">
        /// Zero based index from which to consider the start of the string
        /// </param>
        /// <param name="nextIndex">
        /// Out parameter that points to the starting index of the next string occurrence
        /// in the same byte[], if exists
        /// </param>
        /// <returns>The resulting decoded string</returns>
        public static string GetString (this byte[] bytes, int index, out int nextIndex)
		{
			var length = bytes.GetStringLenght (index);

			nextIndex = index + MqttProtocol.StringPrefixLength + length;

			return length == 0 ? string.Empty : Encoding.UTF8.GetString (bytes, index + MqttProtocol.StringPrefixLength, length);
		}

        /// <summary>
        /// Converts a byte[] to a 16-bit unsigned integer,
        /// taking the byte order ("endiannes") into account
        /// </summary>
        /// <returns>The resulting 16-bit unsigned integer</returns>
		public static ushort ToUInt16 (this byte[] bytes)
		{
			if (BitConverter.IsLittleEndian) {
				Array.Reverse (bytes);
			}

			return BitConverter.ToUInt16 (bytes, 0);
		}

		static ushort GetStringLenght (this byte[] bytes, int index)
		{
			return bytes.Bytes (index, 2).ToUInt16 ();
		}
	}
}
