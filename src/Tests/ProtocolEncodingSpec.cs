using System;
using System.Collections.Generic;
using System.Net.Mqtt;
using Xunit;

namespace Tests
{
	public class ProtocolEncodingSpec
	{
		[Fact]
		public void when_encoding_string_then_prefix_length_is_added()
		{
			var text = "Foo";
			var encoded = MqttProtocol.Encoding.EncodeString (text);

			Assert.Equal (MqttProtocol.StringPrefixLength + text.Length, encoded.Length);
			Assert.Equal (0x00, encoded[0]);
			Assert.Equal (0x03, encoded[1]);
		}

		[Fact]
		public void when_encoding_string_with_exceeded_length_then_fails()
		{
			var text = GetRandomString (size: 65537);

			Assert.Throws<MqttException>(() => MqttProtocol.Encoding.EncodeString (text));
		}

		[Fact]
		public void when_encoding_int32_minor_than_max_protocol_length_then_is_encoded_big_endian()
		{
			var number = 35000; //00000000 00000000 10001000 10111000
			var encoded = MqttProtocol.Encoding.EncodeInteger (number);

			Assert.Equal (2, encoded.Length);
			Assert.Equal (Convert.ToByte ("10001000", fromBase: 2), encoded[0]);
			Assert.Equal (Convert.ToByte ("10111000", fromBase: 2), encoded[1]);
		}

		[Fact]
		public void when_encoding_uint16_then_succeeds_is_encoded_big_endian()
		{
			ushort number = 35000; //10001000 10111000
			var encoded = MqttProtocol.Encoding.EncodeInteger (number);

			Assert.Equal (2, encoded.Length);
			Assert.Equal (Convert.ToByte ("10001000", fromBase: 2), encoded[0]);
			Assert.Equal (Convert.ToByte ("10111000", fromBase: 2), encoded[1]);
		}
		
		[Fact]
		public void when_encoding_int32_major_than_max_protocol_length_then_fails()
		{
			var number = 310934; //00000000 00000100 10111110 10010110
			
			Assert.Throws<MqttException>(() => MqttProtocol.Encoding.EncodeInteger (number));
		}

		[Fact]
		public void when_encoding_remaining_length_then_succeeds()
		{
			//According to spec samples: http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html#_Toc385349213

			var length1From = 0; //00000000
			var length1To = 127; //01111111

			var length2From = 128; //00000000 10000000
			var length2To = 16383; //00111111 11111111

			var length3From = 16384; //00000000 01000000 00000000
			var length3To = 2097151; //00011111 11111111 11111111

			var length4From = 2097152; //00000000 00100000 00000000 00000000
			var length4To = 268435455; //00001111 11111111 11111111 11111111

			var length5 = 64; //01000000
			var length6 = 321; //00000001 01000001
			
			var encoded1From = MqttProtocol.Encoding.EncodeRemainingLength (length1From);
			var encoded1To = MqttProtocol.Encoding.EncodeRemainingLength (length1To);

			var encoded2From = MqttProtocol.Encoding.EncodeRemainingLength (length2From);
			var encoded2To = MqttProtocol.Encoding.EncodeRemainingLength (length2To);

			var encoded3From = MqttProtocol.Encoding.EncodeRemainingLength (length3From);
			var encoded3To = MqttProtocol.Encoding.EncodeRemainingLength (length3To);

			var encoded4From = MqttProtocol.Encoding.EncodeRemainingLength (length4From);
			var encoded4To = MqttProtocol.Encoding.EncodeRemainingLength (length4To);

			var encoded5 = MqttProtocol.Encoding.EncodeRemainingLength (length5); //0x40
			var encoded6 = MqttProtocol.Encoding.EncodeRemainingLength (length6); //193 2

			Assert.Single (encoded1From);
			Assert.Equal (0x00, encoded1From[0]);
			Assert.Single (encoded1To);
			Assert.Equal (0x7F, encoded1To[0]);

			Assert.Equal (2, encoded2From.Length);
			Assert.Equal (0x80, encoded2From[0]);
			Assert.Equal (0x01, encoded2From[1]);
			Assert.Equal (2, encoded2To.Length);
			Assert.Equal (0xFF, encoded2To[0]);
			Assert.Equal (0x7F, encoded2To[1]);

			Assert.Equal (3, encoded3From.Length);
			Assert.Equal (0x80, encoded3From[0]);
			Assert.Equal (0x80, encoded3From[1]);
			Assert.Equal (0x01, encoded3From[2]);
			Assert.Equal (3, encoded3To.Length);
			Assert.Equal (0xFF, encoded3To[0]);
			Assert.Equal (0xFF, encoded3To[1]);
			Assert.Equal (0x7F, encoded3To[2]);

			Assert.Equal (4, encoded4From.Length);
			Assert.Equal (0x80, encoded4From[0]);
			Assert.Equal (0x80, encoded4From[1]);
			Assert.Equal (0x80, encoded4From[2]);
			Assert.Equal (0x01, encoded4From[3]);
			Assert.Equal (4, encoded4To.Length);
			Assert.Equal (0xFF, encoded4To[0]);
			Assert.Equal (0xFF, encoded4To[1]);
			Assert.Equal (0xFF, encoded4To[2]);
			Assert.Equal (0x7F, encoded4To[3]);

			Assert.Single (encoded5);
			Assert.Equal (0x40, encoded5[0]);

			Assert.Equal (2, encoded6.Length);
			Assert.Equal (193, encoded6[0]);
			Assert.Equal (2, encoded6[1]);
		}

		[Fact]
		public void when_decoding_remaining_length_then_succeeds()
		{
			//According to spec samples: http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html#_Toc385349213

			var encoded1From = new List<byte> ();

			encoded1From.Add (0x10);
			encoded1From.Add (0x00);

			var encoded1To = new List<byte> ();

			encoded1To.Add (0x10);
			encoded1To.Add (0x7F);

			var encoded2From = new List<byte> ();

			encoded2From.Add (0x10);
			encoded2From.Add (0x80);
			encoded2From.Add (0x01);

			var encoded2To = new List<byte> ();

			encoded2To.Add (0x10);
			encoded2To.Add (0xFF);
			encoded2To.Add (0x7F);

			var encoded3From = new List<byte> ();

			encoded3From.Add (0x10);
			encoded3From.Add (0x80);
			encoded3From.Add (0x80);
			encoded3From.Add (0x01);

			var encoded3To = new List<byte> ();

			encoded3To.Add (0x10);
			encoded3To.Add (0xFF);
			encoded3To.Add (0xFF);
			encoded3To.Add (0x7F);

			var encoded4From = new List<byte> ();

			encoded4From.Add (0x10);
			encoded4From.Add (0x80);
			encoded4From.Add (0x80);
			encoded4From.Add (0x80);
			encoded4From.Add (0x01);

			var encoded4To = new List<byte> ();

			encoded4To.Add (0x10);
			encoded4To.Add (0xFF);
			encoded4To.Add (0xFF);
			encoded4To.Add (0xFF);
			encoded4To.Add (0x7F);

			var bytes1 = new List<byte> ();

			bytes1.Add (0x10);
			bytes1.Add (0x40);

			var bytes2 = new List<byte> ();

			bytes2.Add (0x10);
			bytes2.Add (193);
			bytes2.Add (2);

			var arrayLength1From = 0;
			var length1From = MqttProtocol.Encoding.DecodeRemainingLength (encoded1From.ToArray(), out arrayLength1From); //0
			var arrayLength1To = 0;
			var length1To = MqttProtocol.Encoding.DecodeRemainingLength (encoded1To.ToArray(), out arrayLength1To); //127
			
			var arrayLength2From = 0;
			var length2From = MqttProtocol.Encoding.DecodeRemainingLength (encoded2From.ToArray(), out arrayLength2From); //128
			var arrayLength2To = 0;
			var length2To = MqttProtocol.Encoding.DecodeRemainingLength (encoded2To.ToArray(), out arrayLength2To); //16383

			var arrayLength3From = 0;
			var length3From = MqttProtocol.Encoding.DecodeRemainingLength (encoded3From.ToArray(), out arrayLength3From); //16384
			var arrayLength3To = 0;
			var length3To = MqttProtocol.Encoding.DecodeRemainingLength (encoded3To.ToArray(), out arrayLength3To); //2097151

			var arrayLength4From = 0;
			var length4From = MqttProtocol.Encoding.DecodeRemainingLength (encoded4From.ToArray(), out arrayLength4From); //2097152
			var arrayLength4To = 0;
			var length4To = MqttProtocol.Encoding.DecodeRemainingLength (encoded4To.ToArray(), out arrayLength4To); //268435455

			var arrayLength1 = 0;
			var length1 = MqttProtocol.Encoding.DecodeRemainingLength (bytes1.ToArray(), out arrayLength1); //64
			var arrayLength2 = 0;
			var length2 = MqttProtocol.Encoding.DecodeRemainingLength (bytes2.ToArray(), out arrayLength2); //321

			Assert.Equal (1, arrayLength1From);
			Assert.Equal(0, length1From);
			Assert.Equal (1, arrayLength1To);
			Assert.Equal(127, length1To);

			Assert.Equal (2, arrayLength2From);
			Assert.Equal(128, length2From);
			Assert.Equal (2, arrayLength2To);
			Assert.Equal(16383, length2To);

			Assert.Equal (3, arrayLength3From);
			Assert.Equal(16384, length3From);
			Assert.Equal (3, arrayLength3To);
			Assert.Equal(2097151 , length3To);

			Assert.Equal (4, arrayLength4From);
			Assert.Equal(2097152 , length4From);
			Assert.Equal (4, arrayLength4To);
			Assert.Equal(268435455 , length4To);

			Assert.Equal (1, arrayLength1);
			Assert.Equal(64, length1);

			Assert.Equal (2, arrayLength2);
			Assert.Equal(321, length2);
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

			Assert.Throws<MqttException> (() => MqttProtocol.Encoding.DecodeRemainingLength (bytes.ToArray (), out arrayLength));
		}

		string GetRandomString(int size)
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
