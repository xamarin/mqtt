using System;
using System.Collections.Generic;
using Hermes;
using Xunit;

namespace Tests
{
	public class ByteExtensionsSpec
	{
		[Fact]
		public void when_setting_bit_then_succeeds()
		{
			var @byte = Convert.ToByte ("00100000", fromBase: 2);
			
			@byte = @byte.Set (3);

			var expectedByte = Convert.ToByte ("00101000", fromBase: 2);

			Assert.Equal (expectedByte, @byte);
		}

		[Fact]
		public void when_unsetting_bit_then_succeeds()
		{
			var @byte = Convert.ToByte ("00100000", fromBase: 2);
			
			@byte = @byte.Unset (5);

			Assert.Equal ((byte)0x00, @byte);
		}

		[Fact]
		public void when_verifying_bit_set_then_succeeds()
		{
			var @byte = Convert.ToByte ("00100000", fromBase: 2);
			
			@byte = @byte.Set (3);

			Assert.True(@byte.IsSet(3));
		}

		[Fact]
		public void when_toggling_bit_then_succeeds()
		{
			var @byte = Convert.ToByte ("00101000", fromBase: 2);
			var byte2 = Convert.ToByte ("00100000", fromBase: 2);

			@byte = @byte.Toggle (3);
			byte2 = byte2.Toggle (3);

			var expectedByte = Convert.ToByte ("00100000", fromBase: 2);
			var expectedByte2 = Convert.ToByte ("00101000", fromBase: 2);

			Assert.Equal (expectedByte, @byte);
			Assert.Equal (expectedByte2, byte2);
		}

		[Fact]
		public void when_getting_bits_then_succeeds()
		{
			var @byte = Convert.ToByte ("00101000", fromBase: 2);
			var newByte1 = @byte.Bits (4); //00000010
			var number1 = Convert.ToInt32 (newByte1);
			var newByte2 = @byte.Bits (3, 3); //00000101
			var number2 = Convert.ToInt32 (newByte2);

			Assert.Equal (2, number1);
			Assert.Equal (5, number2);
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
			bytes.AddRange(ProtocolEncoding.EncodeString("Foo"));
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
			bytes.AddRange(ProtocolEncoding.EncodeString("Foo"));
            bytes.Add(0xA9);
            bytes.Add(Convert.ToByte("00000000", 2));

			var nextIndex = 0;
			var text = bytes.ToArray ().GetString (2, out nextIndex);

			Assert.Equal ("Foo", text);
			Assert.Equal (7, nextIndex);
		}
	}
}
