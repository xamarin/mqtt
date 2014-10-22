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
	public class SubscribeAckFormatterSpec
	{
		private readonly Mock<IChannel<IMessage>> messageChannel;
		private readonly Mock<IChannel<byte[]>> byteChannel;

		public SubscribeAckFormatterSpec ()
		{
			this.messageChannel = new Mock<IChannel<IMessage>> ();
			this.byteChannel = new Mock<IChannel<byte[]>> ();
		}
		
		[Theory]
		[InlineData("Files/SubscribeAck_SingleTopic.packet", "Files/SubscribeAck_SingleTopic.json")]
		[InlineData("Files/SubscribeAck_MultiTopic.packet", "Files/SubscribeAck_MultiTopic.json")]
		public async Task when_reading_subscribe_ack_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedSubscribeAck = Packet.ReadMessage<SubscribeAck> (jsonPath);
			var sentSubscribeAck = default(SubscribeAck);

			this.messageChannel
				.Setup (c => c.SendAsync (It.IsAny<IMessage>()))
				.Returns(Task.Delay(0))
				.Callback<IMessage>(m =>  {
					sentSubscribeAck = m as SubscribeAck;
				});

			var formatter = new SubscribeAckFormatter (this.messageChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);

			await formatter.ReadAsync (packet);

			Assert.Equal (expectedSubscribeAck, sentSubscribeAck);
		}

		[Theory]
		[InlineData("Files/SubscribeAck_SingleTopic.json", "Files/SubscribeAck_SingleTopic.packet")]
		[InlineData("Files/SubscribeAck_MultiTopic.json", "Files/SubscribeAck_MultiTopic.packet")]
		public async Task when_writing_subscribe_ack_packet_then_succeeds(string jsonPath, string packetPath)
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

			var formatter = new SubscribeAckFormatter (this.messageChannel.Object, this.byteChannel.Object);
			var subscribeAck = Packet.ReadMessage<SubscribeAck> (jsonPath);

			await formatter.WriteAsync (subscribeAck);

			Assert.Equal (expectedPacket, sentPacket);
		}
	}
}
