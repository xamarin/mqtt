using System;
using System.IO;
using System.Net.Mqtt;
using System.Net.Mqtt.Sdk.Formatters;
using System.Net.Mqtt.Sdk.Packets;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Formatters
{
	public class ConnectFormatterSpec
	{
		[Theory]
		[InlineData("Files/Binaries/Connect_Full.packet", "Files/Packets/Connect_Full.json")]
		[InlineData("Files/Binaries/Connect_Min.packet", "Files/Packets/Connect_Min.json")]
		public async Task when_reading_connect_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedConnect = Packet.ReadPacket<Connect> (jsonPath);
			var formatter = new ConnectFormatter ();
			var packet = Packet.ReadAllBytes (packetPath);

			var result = await formatter.FormatAsync (packet)
				.ConfigureAwait(continueOnCapturedContext: false);

			Assert.Equal (expectedConnect, result);
		}

		[Theory]
		[InlineData("Files/Binaries/Connect_Invalid_HeaderFlag.packet")]
		[InlineData("Files/Binaries/Connect_Invalid_ProtocolName.packet")]
		[InlineData("Files/Binaries/Connect_Invalid_ConnectReservedFlag.packet")]
		[InlineData("Files/Binaries/Connect_Invalid_QualityOfService.packet")]
		[InlineData("Files/Binaries/Connect_Invalid_WillFlags.packet")]
		[InlineData("Files/Binaries/Connect_Invalid_UserNamePassword.packet")]
		[InlineData("Files/Binaries/Connect_Invalid_ProtocolLevel.packet")]
		public void when_reading_invalid_connect_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new ConnectFormatter ();
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.FormatAsync (packet).Wait());

			Assert.True (ex.InnerException is MqttException);
		}

		[Theory]
		[InlineData("Files/Binaries/Connect_Invalid_ClientIdEmptyAndNoCleanSession.packet")]
		[InlineData("Files/Binaries/Connect_Invalid_ClientIdBadFormat.packet")]
        public void when_reading_invalid_client_id_in_connect_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new ConnectFormatter ();
			var packet = Packet.ReadAllBytes (packetPath);

			var ex = Assert.Throws<AggregateException> (() => formatter.FormatAsync (packet).Wait());

			Assert.True (ex.InnerException is MqttConnectionException);
		}

		[Theory]
		[InlineData("Files/Packets/Connect_Full.json", "Files/Binaries/Connect_Full.packet")]
		[InlineData("Files/Packets/Connect_Min.json", "Files/Binaries/Connect_Min.packet")]
		public async Task when_writing_connect_packet_then_succeeds(string jsonPath, string packetPath)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var expectedPacket = Packet.ReadAllBytes (packetPath);
			var formatter = new ConnectFormatter ();
			var connect = Packet.ReadPacket<Connect> (jsonPath);

			var result = await formatter.FormatAsync (connect)
				.ConfigureAwait(continueOnCapturedContext: false);

			Assert.Equal (expectedPacket, result);
		}

		[Theory]
		[InlineData("Files/Packets/Connect_Invalid_UserNamePassword.json")]
		[InlineData("Files/Packets/Connect_Invalid_ClientIdBadFormat.json")]
		[InlineData("Files/Packets/Connect_Invalid_ClientIdInvalidLength.json")]
		public void when_writing_invalid_connect_packet_then_fails(string jsonPath)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var formatter = new ConnectFormatter ();
			var connect = Packet.ReadPacket<Connect> (jsonPath);

			var ex = Assert.Throws<AggregateException> (() => formatter.FormatAsync (connect).Wait());

			Assert.True (ex.InnerException is MqttException);
		}
	}
}
