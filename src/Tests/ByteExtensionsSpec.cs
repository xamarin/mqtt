using System;
using System.Collections.Generic;
using System.Net.Mqtt;
using System.Net.Mqtt.Sdk;
using Xunit;

namespace Tests
{
	public class ByteExtensionsSpec
	{
		[Fact]
		public void when_setting_bits_then_succeeds()
		{
			var @byte1 = Convert.ToByte ("00100000", fromBase: 2);
			var @byte2 = Convert.ToByte ("10000000", fromBase: 2);
			var @byte3 = Convert.ToByte ("00000010", fromBase: 2);
			var @byte4 = Convert.ToByte ("11111110", fromBase: 2);

			@byte1 = @byte1.Set (3);
			@byte2 = @byte2.Set (1);
			@byte3 = @byte3.Set (2);
			@byte4 = @byte4.Set (0);

			var expectedByte1 = Convert.ToByte ("00101000", fromBase: 2);
			var expectedByte2 = Convert.ToByte ("10000010", fromBase: 2);
			var expectedByte3 = Convert.ToByte ("00000110", fromBase: 2);
			var expectedByte4 = Convert.ToByte ("11111111", fromBase: 2);

			Assert.Equal (expectedByte1, @byte1);
			Assert.Equal (expectedByte2, @byte2);
			Assert.Equal (expectedByte3, @byte3);
			Assert.Equal (expectedByte4, @byte4);
		}

		[Fact]
		public void when_setting_bits_out_of_range_then_fails()
		{
			var @byte1 = Convert.ToByte ("00100000", fromBase: 2);

			Assert.Throws<ArgumentOutOfRangeException> (() => @byte1 = @byte1.Set (8));
		}

		[Fact]
		public void when_unsetting_bits_then_succeeds()
		{
			var @byte1 = Convert.ToByte ("00100000", fromBase: 2);
			var @byte2 = Convert.ToByte ("11111111", fromBase: 2);
			var @byte3 = Convert.ToByte ("10100010", fromBase: 2);
			var @byte4 = Convert.ToByte ("00000001", fromBase: 2);

			@byte1 = @byte1.Unset (5);
			@byte2 = @byte2.Unset (7);
			@byte3 = @byte3.Unset (1);
			@byte4 = @byte4.Unset (0);

			Assert.Equal ((byte)0x00, @byte1);
			Assert.Equal ((byte)0x7F, @byte2);
			Assert.Equal ((byte)0xA0, @byte3);
			Assert.Equal ((byte)0x00, @byte4);
		}

		[Fact]
		public void when_unsetting_bits_out_of_range_then_fails()
		{
			var @byte1 = Convert.ToByte ("00100000", fromBase: 2);

			Assert.Throws<ArgumentOutOfRangeException> (() => @byte1 = @byte1.Unset (8));
		}

		[Fact]
		public void when_verifying_bit_set_then_succeeds()
		{
			var @byte = Convert.ToByte ("00100000", fromBase: 2);
			
			@byte = @byte.Set (3);

			Assert.True(@byte.IsSet(3));
		}

		[Fact]
		public void when_verifying_bit_out_of_range_set_then_fails()
		{
			var @byte = Convert.ToByte ("00100000", fromBase: 2);
			
			@byte = @byte.Set (3);

			Assert.Throws<ArgumentOutOfRangeException> (() => @byte.IsSet(8));
		}

		[Fact]
		public void when_getting_bits_then_succeeds()
		{
			var @byte1 = Convert.ToByte ("11101110", fromBase: 2);
			var @byte2 = Convert.ToByte ("11111111", fromBase: 2);
			var @byte3 = Convert.ToByte ("00000011", fromBase: 2);
			var @byte4 = Convert.ToByte ("00110011", fromBase: 2);

			Assert.Equal (14, Convert.ToInt32 (@byte1.Bits (4))); //00001110
			Assert.Equal (1, Convert.ToInt32 (@byte1.Bits (4, 2))); //00000001
			Assert.Equal (127, Convert.ToInt32 (@byte2.Bits (7))); //01111111
			Assert.Equal (3, Convert.ToInt32 (@byte2.Bits (7, 2))); //00000011
			Assert.Equal (0, Convert.ToInt32 (@byte3.Bits (3))); //00000000
			Assert.Equal (1, Convert.ToInt32 (@byte3.Bits (1, 7))); //00000001
			Assert.Equal (1, Convert.ToInt32 (@byte4.Bits (3))); //00000001
			Assert.Equal (6, Convert.ToInt32 (@byte4.Bits (1, 5))); //00000110
		}

		[Fact]
		public void when_getting_bits_with_index_out_of_range_then_fails()
		{
			var @byte1 = Convert.ToByte ("11101110", fromBase: 2);

			Assert.Throws<ArgumentOutOfRangeException> (() => (@byte1.Bits (0, 2)));
			Assert.Throws<ArgumentOutOfRangeException> (() => (@byte1.Bits (9, 1)));
		}

		[Fact]
		public void when_getting_byte_then_succeeds()
		{
			var bytes = new List<byte>();

			bytes.Add(Convert.ToByte ("00101000", fromBase: 2));
			bytes.Add(0xFE);
            bytes.Add(0xA9);
            bytes.Add(Convert.ToByte("00000000", 2));

			var @byte = bytes.ToArray ().Byte (2);

			Assert.Equal (0XA9, @byte);
		}

		[Fact]
		public void when_getting_bytes_then_succeeds()
		{
			var bytes = new List<byte>();

			bytes.Add(Convert.ToByte ("00101000", fromBase: 2));
			bytes.Add(0xFE);
            bytes.Add(0xA9);
            bytes.Add(Convert.ToByte("00000000", 2));

			var newBytes = bytes.ToArray ().Bytes (1, 2);

			Assert.Equal (2, newBytes.Length);
			Assert.Equal (0xFE, newBytes[0]);
			Assert.Equal (0xA9, newBytes[1]);
		}

		[Fact]
		public void when_getting_string_then_succeeds()
		{
			var bytes = new List<byte>();

			bytes.Add(Convert.ToByte ("00101000", fromBase: 2));
			bytes.Add(0xFE);
			bytes.AddRange(MqttProtocol.Encoding.EncodeString("Foo"));
            bytes.Add(0xA9);
            bytes.Add(Convert.ToByte("00000000", 2));

			var text = bytes.ToArray ().GetString (2);

			Assert.Equal ("Foo", text);
		}

		[Fact]
		public void when_getting_string_with_output_then_succeeds()
		{
			var bytes = new List<byte>();

			bytes.Add(Convert.ToByte ("00101000", fromBase: 2));
			bytes.Add(0xFE);
			bytes.AddRange(MqttProtocol.Encoding.EncodeString("Foo"));
            bytes.Add(0xA9);
            bytes.Add(Convert.ToByte("00000000", 2));

			var nextIndex = 0;
			var text = bytes.ToArray ().GetString (2, out nextIndex);

			Assert.Equal ("Foo", text);
			Assert.Equal (7, nextIndex);
		}
	}
}
