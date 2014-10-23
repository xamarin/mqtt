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
	public class PublishFormatterSpec
	{
		readonly Mock<IChannel<IMessage>> messageChannel;
		readonly Mock<IChannel<byte[]>> byteChannel;

		public PublishFormatterSpec ()
		{
			this.messageChannel = new Mock<IChannel<IMessage>> ();
			this.byteChannel = new Mock<IChannel<byte[]>> ();
		}
		
		[Theory]
		[InlineData("Files/Packets/Publish_Full.packet", "Files/Messages/Publish_Full.json")]
		[InlineData("Files/Packets/Publish_Min.packet", "Files/Messages/Publish_Min.json")]
		public async Task when_reading_publish_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedPublish = Packet.ReadMessage<Publish> (jsonPath);
			var sentPublish = default(Publish);

			this.messageChannel
				.Setup (c => c.SendAsync (It.IsAny<IMessage>()))
				.Returns(Task.Delay(0))
				.Callback<IMessage>(m =>  {
					sentPublish = m as Publish;
				});

			var formatter = new PublishFormatter (this.messageChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);

			await formatter.ReadAsync (packet);

			Assert.Equal (expectedPublish, sentPublish);
		}

		[Theory]
		[InlineData("Files/Packets/Publish_Invalid_QualityOfService.packet")]
		[InlineData("Files/Packets/Publish_Invalid_Duplicated.packet")]
		[InlineData("Files/Packets/Publish_Invalid_Topic.packet")]
		public void when_reading_invalid_publish_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new PublishFormatter (this.messageChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.ReadAsync (packet).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}

		[Theory]
		[InlineData("Files/Messages/Publish_Full.json", "Files/Packets/Publish_Full.packet")]
		[InlineData("Files/Messages/Publish_Min.json", "Files/Packets/Publish_Min.packet")]
		public async Task when_writing_publish_packet_then_succeeds(string jsonPath, string packetPath)
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

			var formatter = new PublishFormatter (this.messageChannel.Object, this.byteChannel.Object);
			var publish = Packet.ReadMessage<Publish> (jsonPath);

			await formatter.WriteAsync (publish);

			Assert.Equal (expectedPacket, sentPacket);
		}

		[Theory]
		[InlineData("Files/Messages/Publish_Invalid_Duplicated.json")]
		[InlineData("Files/Messages/Publish_Invalid_Topic.json")]
		[InlineData("Files/Messages/Publish_Invalid_MessageId.json")]
		public void when_writing_invalid_publish_packet_then_fails(string jsonPath)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var formatter = new PublishFormatter (this.messageChannel.Object, this.byteChannel.Object);
			var publish = Packet.ReadMessage<Publish> (jsonPath);

			var ex = Assert.Throws<AggregateException> (() => formatter.WriteAsync (publish).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}
	}
}
