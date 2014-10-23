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
	public class PublishAckFormatterSpec
	{
		readonly Mock<IChannel<IMessage>> messageChannel;
		readonly Mock<IChannel<byte[]>> byteChannel;

		public PublishAckFormatterSpec ()
		{
			this.messageChannel = new Mock<IChannel<IMessage>> ();
			this.byteChannel = new Mock<IChannel<byte[]>> ();
		}

		[Theory]
		[InlineData("Files/Packets/PublishAck.packet", "Files/Messages/PublishAck.json")]
		public async Task when_reading_publish_ack_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedPublishAck = Packet.ReadMessage<PublishAck> (jsonPath);
			var sentPublishAck = default(PublishAck);

			this.messageChannel
				.Setup (c => c.SendAsync (It.IsAny<IMessage>()))
				.Returns(Task.Delay(0))
				.Callback<IMessage>(m =>  {
					sentPublishAck = m as PublishAck;
				});

			var formatter = new FlowMessageFormatter<PublishAck>(MessageType.PublishAck, id => new PublishAck(id), this.messageChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);

			await formatter.ReadAsync (packet);

			Assert.Equal (expectedPublishAck, sentPublishAck);
		}

		[Theory]
		[InlineData("Files/Packets/PublishAck_Invalid_HeaderFlag.packet")]
		public void when_reading_invalid_publish_ack_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new FlowMessageFormatter<PublishAck> (MessageType.PublishAck, id => new PublishAck(id), this.messageChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.ReadAsync (packet).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}

		[Theory]
		[InlineData("Files/Messages/PublishAck.json", "Files/Packets/PublishAck.packet")]
		public async Task when_writing_publish_ack_packet_then_succeeds(string jsonPath, string packetPath)
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

			var formatter = new FlowMessageFormatter<PublishAck>(MessageType.PublishAck, id => new PublishAck(id), this.messageChannel.Object, this.byteChannel.Object);
			var publishAck = Packet.ReadMessage<PublishAck> (jsonPath);

			await formatter.WriteAsync (publishAck);

			Assert.Equal (expectedPacket, sentPacket);
		}
	}
}
