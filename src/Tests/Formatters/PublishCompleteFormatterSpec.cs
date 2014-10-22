using System;
using System.IO;
using System.Threading.Tasks;
using Hermes;
using Hermes.Formatters;
using Hermes.Messages;
using Moq;
using Xunit;
using Xunit.Extensions;

namespace Tests.Formatters
{
	public class PublishCompleteFormatterSpec
	{
		private readonly Mock<IChannel<IMessage>> messageChannel;
		private readonly Mock<IChannel<byte[]>> byteChannel;

		public PublishCompleteFormatterSpec ()
		{
			this.messageChannel = new Mock<IChannel<IMessage>> ();
			this.byteChannel = new Mock<IChannel<byte[]>> ();
		}

		[Theory]
		[InlineData("Files/PublishComplete.packet", "Files/PublishComplete.json")]
		public async Task when_reading_publish_complete_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedPublishComplete = Packet.ReadMessage<PublishComplete> (jsonPath);
			var sentPublishComplete = default(PublishComplete);

			this.messageChannel
				.Setup (c => c.SendAsync (It.IsAny<IMessage>()))
				.Returns(Task.Delay(0))
				.Callback<IMessage>(m =>  {
					sentPublishComplete = m as PublishComplete;
				});

			var formatter = new FlowMessageFormatter<PublishComplete>(MessageType.PublishComplete, id => new PublishComplete(id), this.messageChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);

			await formatter.ReadAsync (packet);

			Assert.Equal (expectedPublishComplete, sentPublishComplete);
		}

		[Theory]
		[InlineData("Files/PublishComplete.json", "Files/PublishComplete.packet")]
		public async Task when_writing_publish_complete_packet_then_succeeds(string jsonPath, string packetPath)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var expectedPacket = Packet.ReadAllBytes (packetPath);
			var sentPacket = default(byte[]);

			this.byteChannel
				.Setup (c => c.SendAsync (It.IsAny<byte[]>()))
				.Returns(Task.Delay(0))
				.Callback<byte[]>(b =>  {
					sentPacket = b;
				});

			var formatter = new FlowMessageFormatter<PublishComplete>(MessageType.PublishComplete, id => new PublishComplete(id), this.messageChannel.Object, this.byteChannel.Object);
			var publishComplete = Packet.ReadMessage<PublishComplete> (jsonPath);

			await formatter.WriteAsync (publishComplete);

			Assert.Equal (expectedPacket, sentPacket);
		}
	}
}
