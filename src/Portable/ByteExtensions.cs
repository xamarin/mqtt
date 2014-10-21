using System;
using System.Linq;
using System.Text;
using Hermes.Properties;

namespace Hermes
{
	public static class ByteExtensions
	{
		public static bool IsSet(this byte @byte, int bit)
        {
            if (bit > 7)
                throw new ArgumentOutOfRangeException("bit", Resources.ByteExtensions_InvalidBitPosition);

            return (@byte & (1 << bit)) != 0;
        }

        public static byte Set(this byte @byte, int bit)
        {
            if (bit > 7)
                throw new ArgumentOutOfRangeException("bit", Resources.ByteExtensions_InvalidBitPosition);

            return Convert.ToByte(@byte | (1 << bit));
        }

        public static byte Unset(this byte @byte, int bit)
        {
            if (bit > 7)
                throw new ArgumentOutOfRangeException("bit", Resources.ByteExtensions_InvalidBitPosition);

            return Convert.ToByte(@byte & ~(1 << bit));
        }

        public static byte Toggle(this byte @byte, int bit)
        {
            if (bit > 7)
                throw new ArgumentOutOfRangeException("bit", Resources.ByteExtensions_InvalidBitPosition);

            return Convert.ToByte(@byte ^ (1 << bit));
        }

        public static byte Bits(this byte @byte, int count)
        {
            return Convert.ToByte(@byte >> 8 - count);
        }

        public static byte Bits(this byte @byte, int index, int count)
        {
			if (index < 1 || index > 8)
				throw new ArgumentOutOfRangeException("index", Resources.ByteExtensions_InvalidByteIndex);

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

		public static byte Byte(this byte[] bytes, int index)
		{
			return bytes.Skip (index).Take (1).FirstOrDefault();
		}

		public static byte[] Bytes(this byte[] bytes, int fromIndex, int count = 0)
		{
			var result = bytes.Skip (fromIndex);
			
			if(count > 0) {
				result = result.Take (count).ToArray();
			}

			return result.ToArray();
		}

		public static string GetString(this byte[] bytes, int index)
		{
			var length = bytes.GetStringLenght (index);
			
			return Encoding.UTF8.GetString (bytes, index + MQTT.StringPrefixLength, length);
		}

		public static string GetString(this byte[] bytes, int index, out int nextIndex)
		{
			var length = bytes.GetStringLenght (index);

			nextIndex = index + MQTT.StringPrefixLength + length;

			return Encoding.UTF8.GetString (bytes, index + MQTT.StringPrefixLength, length);
		}

		public static ushort ToUInt16 (this byte[] bytes)
		{
			if (BitConverter.IsLittleEndian) {
				Array.Reverse (bytes);
			}

			return BitConverter.ToUInt16 (bytes, 0);
		}

		private static ushort GetStringLenght(this byte[] bytes, int index)
		{
			return bytes.Bytes (index, 2).ToUInt16 ();
		}
	}
}
