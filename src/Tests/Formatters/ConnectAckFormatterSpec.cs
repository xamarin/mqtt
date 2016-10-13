using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Mqtt;
using System.Net.Mqtt.Sdk.Formatters;
using System.Net.Mqtt.Sdk.Packets;
using Xunit;
using Xunit.Extensions;

namespace Tests.Formatters
{
	public class ConnectAckFormatterSpec
	{
		[Theory]
		[InlineData("Files/Binaries/ConnectAck.packet", "Files/Packets/ConnectAck.json")]
		public async Task when_reading_connect_ack_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedConnectAck = Packet.ReadPacket<ConnectAck> (jsonPath);
			var formatter = new ConnectAckFormatter ();
			var packet = Packet.ReadAllBytes (packetPath);

			var result = await formatter.FormatAsync (packet)
				.ConfigureAwait(continueOnCapturedContext: false);

			Assert.Equal (expectedConnectAck, result);
		}

		[Theory]
		[InlineData("Files/Binaries/ConnectAck_Invalid_HeaderFlag.packet")]
		[InlineData("Files/Binaries/ConnectAck_Invalid_AckFlags.packet")]
		[InlineData("Files/Binaries/ConnectAck_Invalid_SessionPresent.packet")]
		public void when_reading_invalid_connect_ack_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new ConnectAckFormatter ();
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.FormatAsync (packet).Wait());

			Assert.True (ex.InnerException is MqttException);
		}

		[Theory]
		[InlineData("Files/Packets/ConnectAck.json", "Files/Binaries/ConnectAck.packet")]
		public async Task when_writing_connect_ack_packet_then_succeeds(string jsonPath, string packetPath)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var expectedPacket = Packet.ReadAllBytes (packetPath);
			var formatter = new ConnectAckFormatter ();
			var connectAck = Packet.ReadPacket<ConnectAck> (jsonPath);

			var result = await formatter.FormatAsync (connectAck)
				.ConfigureAwait(continueOnCapturedContext: false);

			Assert.Equal (expectedPacket, result);
		}

		[Theory]
		[InlineData("Files/Packets/ConnectAck_Invalid_SessionPresent.json")]
		public void when_writing_invalid_connect_ack_packet_then_fails(string jsonPath)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var formatter = new ConnectAckFormatter ();
			var connectAck = Packet.ReadPacket<ConnectAck> (jsonPath);

			var ex = Assert.Throws<AggregateException> (() => formatter.FormatAsync (connectAck).Wait());

			Assert.True (ex.InnerException is MqttException);
		}
	}
}
