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
	public class UnsubscribeAckFormatterSpec
	{
		private readonly Mock<IChannel<IMessage>> messageChannel;
		private readonly Mock<IChannel<byte[]>> byteChannel;

		public UnsubscribeAckFormatterSpec ()
		{
			this.messageChannel = new Mock<IChannel<IMessage>> ();
			this.byteChannel = new Mock<IChannel<byte[]>> ();
		}

		[Theory]
		[InlineData("Files/Packets/UnsubscribeAck.packet", "Files/Messages/UnsubscribeAck.json")]
		public async Task when_reading_unsubscribe_ack_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedUnsubscribeAck = Packet.ReadMessage<UnsubscribeAck> (jsonPath);
			var sentUnsubscribeAck = default(UnsubscribeAck);

			this.messageChannel
				.Setup (c => c.SendAsync (It.IsAny<IMessage>()))
				.Returns(Task.Delay(0))
				.Callback<IMessage>(m =>  {
					sentUnsubscribeAck = m as UnsubscribeAck;
				});

			var formatter = new FlowMessageFormatter<UnsubscribeAck>(MessageType.UnsubscribeAck, id => new UnsubscribeAck(id), this.messageChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);

			await formatter.ReadAsync (packet);

			Assert.Equal (expectedUnsubscribeAck, sentUnsubscribeAck);
		}

		[Theory]
		[InlineData("Files/Packets/UnsubscribeAck_Invalid_HeaderFlag.packet")]
		public void when_reading_invalid_unsubscribe_ack_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new FlowMessageFormatter<UnsubscribeAck> (MessageType.UnsubscribeAck, id => new UnsubscribeAck(id), this.messageChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.ReadAsync (packet).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}

		[Theory]
		[InlineData("Files/Messages/UnsubscribeAck.json", "Files/Packets/UnsubscribeAck.packet")]
		public async Task when_writing_unsubscribe_ack_packet_then_succeeds(string jsonPath, string packetPath)
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

			var formatter = new FlowMessageFormatter<UnsubscribeAck>(MessageType.UnsubscribeAck, id => new UnsubscribeAck(id), this.messageChannel.Object, this.byteChannel.Object);
			var unsubscribeAck = Packet.ReadMessage<UnsubscribeAck> (jsonPath);

			await formatter.WriteAsync (unsubscribeAck);

			Assert.Equal (expectedPacket, sentPacket);
		}
	}
}
