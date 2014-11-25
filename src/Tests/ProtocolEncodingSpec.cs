using System;
using System.Collections.Generic;
using Hermes;
using Xunit;

namespace Tests
{
	public class ProtocolEncodingSpec
	{
		[Fact]
		public void when_encoding_string_then_prefix_length_is_added()
		{
			var text = "Foo";
			var encoded = Protocol.Encoding.EncodeString (text);

			Assert.Equal (Protocol.StringPrefixLength + text.Length, encoded.Length);
			Assert.Equal (0x00, encoded[0]);
			Assert.Equal (0x03, encoded[1]);
		}

		[Fact]
		public void when_encoding_string_with_exceeded_length_then_fails()
		{
			var text = this.GetRandomString (size: 65537);

			Assert.Throws<ProtocolException>(() => Protocol.Encoding.EncodeString (text));
		}

		[Fact]
		public void when_encoding_int32_minor_than_max_protocol_length_then_is_encoded_big_endian()
		{
			var number = 35000; //00000000 00000000 10001000 10111000
			var encoded = Protocol.Encoding.EncodeInteger (number);

			Assert.Equal (2, encoded.Length);
			Assert.Equal (Convert.ToByte ("10001000", fromBase: 2), encoded[0]);
			Assert.Equal (Convert.ToByte ("10111000", fromBase: 2), encoded[1]);
		}

		[Fact]
		public void when_encoding_uint16_then_succeeds_is_encoded_big_endian()
		{
			ushort number = 35000; //10001000 10111000
			var encoded = Protocol.Encoding.EncodeInteger (number);

			Assert.Equal (2, encoded.Length);
			Assert.Equal (Convert.ToByte ("10001000", fromBase: 2), encoded[0]);
			Assert.Equal (Convert.ToByte ("10111000", fromBase: 2), encoded[1]);
		}
		
		[Fact]
		public void when_encoding_int32_major_than_max_protocol_length_then_fails()
		{
			var number = 310934; //00000000 00000100 10111110 10010110
			
			Assert.Throws<ProtocolException>(() => Protocol.Encoding.EncodeInteger (number));
		}

		[Fact]
		public void when_encoding_remaining_length_then_succeeds()
		{
			var length1 = 64; //01000000
			var length2 = 321; //00000001 01000001
			var length3 = 268435455; //00001111 11111111 11111111 11111111;

			//According to spec samples: http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html#_Toc385349213

			var encoded1 = Protocol.Encoding.EncodeRemainingLength (length1); //0x40
			var encoded2 = Protocol.Encoding.EncodeRemainingLength (length2); //193 2
			var encoded3 = Protocol.Encoding.EncodeRemainingLength (length3); //0xFF 0xFF 0xFF 0x7F

			Assert.Equal (1, encoded1.Length);
			Assert.Equal (0x40, encoded1[0]);

			Assert.Equal (2, encoded2.Length);
			Assert.Equal (193, encoded2[0]);
			Assert.Equal (2, encoded2[1]);

			Assert.Equal (4, encoded3.Length);
			Assert.Equal (0xFF, encoded3[0]);
			Assert.Equal (0xFF, encoded3[1]);
			Assert.Equal (0xFF, encoded3[2]);
			Assert.Equal (0x7F, encoded3[3]);
		}

		[Fact]
		public void when_decoding_remaining_length_then_succeeds()
		{
			var bytes1 = new List<byte> ();

			bytes1.Add (0x10);
			bytes1.Add (0x40);

			var bytes2 = new List<byte> ();

			bytes2.Add (0x10);
			bytes2.Add (193);
			bytes2.Add (2);

			var bytes3 = new List<byte> ();

			bytes3.Add (0x20);
			bytes3.Add (0xFF);
			bytes3.Add (0xFF);
			bytes3.Add (0xFF);
			bytes3.Add (0x7F);

			//According to spec samples: http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html#_Toc385349213

			var arrayLength1 = 0;
			var remainingLength1 = Protocol.Encoding.DecodeRemainingLength (bytes1.ToArray(), out arrayLength1); //64
			var arrayLength2 = 0;
			var remainingLength2 = Protocol.Encoding.DecodeRemainingLength (bytes2.ToArray(), out arrayLength2); //321
			var arrayLength3 = 0;
			var remainingLength3 = Protocol.Encoding.DecodeRemainingLength (bytes3.ToArray(), out arrayLength3); //268435455

			Assert.Equal (1, arrayLength1);
			Assert.Equal(64, remainingLength1);
			Assert.Equal (2, arrayLength2);
			Assert.Equal(321, remainingLength2);
			Assert.Equal (4, arrayLength3);
			Assert.Equal(268435455, remainingLength3);
		}

		[Fact]
		public void when_decoding_malformed_remaining_length_then_fails()
		{
			var bytes = new List<byte> ();

			bytes.Add (0x10);
			bytes.Add (0xFF);
			bytes.Add (0xFF);
			bytes.Add (0xFF);
			bytes.Add (0xFF);
			bytes.Add (0x7F);

			//According to spec samples: http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html#_Toc385349213

			var arrayLength = 0;

			Assert.Throws<ProtocolException> (() => Protocol.Encoding.DecodeRemainingLength (bytes.ToArray (), out arrayLength));
		}

		private string GetRandomString(int size)
		{
			var random = new Random();
			var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
			var buffer = new char[size];

			for (int i = 0; i < size; i++)
			{
				buffer[i] = chars[random.Next(chars.Length)];
			}

			return new string(buffer);
		}
	}
}
