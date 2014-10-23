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
		readonly Mock<IChannel<IMessage>> messageChannel;
		readonly Mock<IChannel<byte[]>> byteChannel;

		public SubscribeAckFormatterSpec ()
		{
			this.messageChannel = new Mock<IChannel<IMessage>> ();
			this.byteChannel = new Mock<IChannel<byte[]>> ();
		}
		
		[Theory]
		[InlineData("Files/Packets/SubscribeAck_SingleTopic.packet", "Files/Messages/SubscribeAck_SingleTopic.json")]
		[InlineData("Files/Packets/SubscribeAck_MultiTopic.packet", "Files/Messages/SubscribeAck_MultiTopic.json")]
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
		[InlineData("Files/Packets/SubscribeAck_Invalid_HeaderFlag.packet")]
		public void when_reading_invalid_subscribe_ack_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new SubscribeAckFormatter (this.messageChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.ReadAsync (packet).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}

		[Theory]
		[InlineData("Files/Packets/SubscribeAck_Invalid_EmptyReturnCodes.packet")]
		[InlineData("Files/Packets/SubscribeAck_Invalid_ReturnCodes.packet")]
		public void when_reading_invalid_return_code_in_subscribe_ack_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new SubscribeAckFormatter (this.messageChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.ReadAsync (packet).Wait());

			Assert.True (ex.InnerException is ViolationProtocolException);
		}

		[Theory]
		[InlineData("Files/Messages/SubscribeAck_SingleTopic.json", "Files/Packets/SubscribeAck_SingleTopic.packet")]
		[InlineData("Files/Messages/SubscribeAck_MultiTopic.json", "Files/Packets/SubscribeAck_MultiTopic.packet")]
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

		[Theory]
		[InlineData("Files/Messages/SubscribeAck_Invalid_EmptyReturnCodes.json")]
		public void when_writing_invalid_subscribe_ack_packet_then_fails(string jsonPath)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var formatter = new SubscribeAckFormatter (this.messageChannel.Object, this.byteChannel.Object);
			var subscribeAck = Packet.ReadMessage<SubscribeAck> (jsonPath);

			var ex = Assert.Throws<AggregateException> (() => formatter.WriteAsync (subscribeAck).Wait());

			Assert.True (ex.InnerException is ViolationProtocolException);
		}
	}
}
