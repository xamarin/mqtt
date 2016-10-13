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
	public class UnsubscribeFormatterSpec
	{
		[Theory]
		[InlineData("Files/Binaries/Unsubscribe_SingleTopic.packet", "Files/Packets/Unsubscribe_SingleTopic.json")]
		[InlineData("Files/Binaries/Unsubscribe_MultiTopic.packet", "Files/Packets/Unsubscribe_MultiTopic.json")]
		public async Task when_reading_unsubscribe_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedUnsubscribe = Packet.ReadPacket<Unsubscribe> (jsonPath);
			var formatter = new UnsubscribeFormatter ();
			var packet = Packet.ReadAllBytes (packetPath);

			var result = await formatter.FormatAsync (packet)
				.ConfigureAwait(continueOnCapturedContext: false);

			Assert.Equal (expectedUnsubscribe, result);
		}

		[Theory]
		[InlineData("Files/Binaries/Unsubscribe_Invalid_HeaderFlag.packet")]
		public void when_reading_invalid_unsubscribe_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new UnsubscribeFormatter ();
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.FormatAsync (packet).Wait());

			Assert.True (ex.InnerException is MqttException);
		}

		[Theory]
		[InlineData("Files/Binaries/Unsubscribe_Invalid_EmptyTopics.packet")]
		public void when_reading_invalid_topic_in_unsubscribe_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new UnsubscribeFormatter ();
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.FormatAsync (packet).Wait());

			Assert.True (ex.InnerException is MqttProtocolViolationException);
		}

		[Theory]
		[InlineData("Files/Packets/Unsubscribe_SingleTopic.json", "Files/Binaries/Unsubscribe_SingleTopic.packet")]
		[InlineData("Files/Packets/Unsubscribe_MultiTopic.json", "Files/Binaries/Unsubscribe_MultiTopic.packet")]
		public async Task when_writing_unsubscribe_packet_then_succeeds(string jsonPath, string packetPath)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var expectedPacket = Packet.ReadAllBytes (packetPath);
			var formatter = new UnsubscribeFormatter ();
			var unsubscribe = Packet.ReadPacket<Unsubscribe> (jsonPath);

			var result = await formatter.FormatAsync (unsubscribe)
				.ConfigureAwait(continueOnCapturedContext: false);

			Assert.Equal (expectedPacket, result);
		}

		[Theory]
		[InlineData("Files/Packets/Unsubscribe_Invalid_EmptyTopics.json")]
		public void when_writing_invalid_unsubscribe_packet_then_fails(string jsonPath)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var formatter = new UnsubscribeFormatter ();
			var unsubscribe = Packet.ReadPacket<Unsubscribe> (jsonPath);

			var ex = Assert.Throws<AggregateException> (() => formatter.FormatAsync (unsubscribe).Wait());

			Assert.True (ex.InnerException is MqttProtocolViolationException);
		}
	}
}
