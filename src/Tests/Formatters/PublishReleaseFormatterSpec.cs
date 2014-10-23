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
	public class PublishReleaseFormatterSpec
	{
		private readonly Mock<IChannel<IMessage>> messageChannel;
		private readonly Mock<IChannel<byte[]>> byteChannel;

		public PublishReleaseFormatterSpec ()
		{
			this.messageChannel = new Mock<IChannel<IMessage>> ();
			this.byteChannel = new Mock<IChannel<byte[]>> ();
		}

		[Theory]
		[InlineData("Files/Packets/PublishRelease.packet", "Files/Messages/PublishRelease.json")]
		public async Task when_reading_publish_release_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedPublishRelease = Packet.ReadMessage<PublishRelease> (jsonPath);
			var sentPublishRelease = default(PublishRelease);

			this.messageChannel
				.Setup (c => c.SendAsync (It.IsAny<IMessage>()))
				.Returns(Task.Delay(0))
				.Callback<IMessage>(m =>  {
					sentPublishRelease = m as PublishRelease;
				});

			var formatter = new FlowMessageFormatter<PublishRelease>(MessageType.PublishRelease, id => new PublishRelease(id), this.messageChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);

			await formatter.ReadAsync (packet);

			Assert.Equal (expectedPublishRelease, sentPublishRelease);
		}

		[Theory]
		[InlineData("Files/Packets/PublishRelease_Invalid_HeaderFlag.packet")]
		public void when_reading_invalid_publish_release_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new FlowMessageFormatter<PublishRelease> (MessageType.PublishRelease, id => new PublishRelease(id), this.messageChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.ReadAsync (packet).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}

		[Theory]
		[InlineData("Files/Messages/PublishRelease.json", "Files/Packets/PublishRelease.packet")]
		public async Task when_writing_publish_release_packet_then_succeeds(string jsonPath, string packetPath)
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

			var formatter = new FlowMessageFormatter<PublishRelease>(MessageType.PublishRelease, id => new PublishRelease(id), this.messageChannel.Object, this.byteChannel.Object);
			var publishRelease = Packet.ReadMessage<PublishRelease> (jsonPath);

			await formatter.WriteAsync (publishRelease);

			Assert.Equal (expectedPacket, sentPacket);
		}
	}
}
