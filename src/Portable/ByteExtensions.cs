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

        public static byte Bits(this byte @byte, int index)
        {
            return Convert.ToByte(@byte >> index);
        }

        public static byte Bits(this byte @byte, int index, int count)
        {
            var to = Convert.ToByte(@byte << 8 - (index + count));
            var from = Convert.ToByte(to >> 8 - count);

            return from;
        }

		public static byte Byte(this byte[] bytes, int index)
		{
			return bytes.Skip (index).Take (1).FirstOrDefault();
		}

		public static byte[] Bytes(this byte[] bytes, int fromIndex, int count)
		{
			return bytes.Skip (fromIndex).Take (count).ToArray();
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

		private static short GetStringLenght(this byte[] bytes, int index)
		{
			var lengthBytes = bytes.Bytes (index, 2);

			if (BitConverter.IsLittleEndian) {
				Array.Reverse (lengthBytes);
			}

			return BitConverter.ToInt16 (lengthBytes, 0);
		}
	}
}
