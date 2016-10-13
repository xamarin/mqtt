using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Mqtt.Sdk.Formatters;
using System.Net.Mqtt.Sdk.Packets;
using Xunit;
using Xunit.Extensions;
using System.Net.Mqtt;

namespace Tests.Formatters
{
	public class PublishCompleteFormatterSpec
	{
		[Theory]
		[InlineData("Files/Binaries/PublishComplete.packet", "Files/Packets/PublishComplete.json")]
		public async Task when_reading_publish_complete_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedPublishComplete = Packet.ReadPacket<PublishComplete> (jsonPath);
			var formatter = new FlowPacketFormatter<PublishComplete>(MqttPacketType.PublishComplete, id => new PublishComplete(id));
			var packet = Packet.ReadAllBytes (packetPath);

			var result = await formatter.FormatAsync (packet)
				.ConfigureAwait(continueOnCapturedContext: false);

			Assert.Equal (expectedPublishComplete, result);
		}

		[Theory]
		[InlineData("Files/Binaries/PublishComplete_Invalid_HeaderFlag.packet")]
		public void when_reading_invalid_publish_complete_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new FlowPacketFormatter<PublishComplete> (MqttPacketType.PublishComplete, id => new PublishComplete(id));
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.FormatAsync (packet).Wait());

			Assert.True (ex.InnerException is MqttException);
		}

		[Theory]
		[InlineData("Files/Packets/PublishComplete.json", "Files/Binaries/PublishComplete.packet")]
		public async Task when_writing_publish_complete_packet_then_succeeds(string jsonPath, string packetPath)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var expectedPacket = Packet.ReadAllBytes (packetPath);
			var formatter = new FlowPacketFormatter<PublishComplete>(MqttPacketType.PublishComplete, id => new PublishComplete(id));
			var publishComplete = Packet.ReadPacket<PublishComplete> (jsonPath);

			var result = await formatter.FormatAsync (publishComplete)
				.ConfigureAwait(continueOnCapturedContext: false);

			Assert.Equal (expectedPacket, result);
		}
	}
}
