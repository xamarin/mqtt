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
	public class UnsubscribeFormatterSpec
	{
		private readonly Mock<IChannel<IMessage>> messageChannel;
		private readonly Mock<IChannel<byte[]>> byteChannel;

		public UnsubscribeFormatterSpec ()
		{
			this.messageChannel = new Mock<IChannel<IMessage>> ();
			this.byteChannel = new Mock<IChannel<byte[]>> ();
		}
		
		[Theory]
		[InlineData("Files/Unsubscribe_SingleTopic.packet", "Files/Unsubscribe_SingleTopic.json")]
		[InlineData("Files/Unsubscribe_MultiTopic.packet", "Files/Unsubscribe_MultiTopic.json")]
		public async Task when_reading_unsubscribe_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedUnsubscribe = Packet.ReadMessage<Unsubscribe> (jsonPath);
			var sentUnsubscribe = default(Unsubscribe);

			this.messageChannel
				.Setup (c => c.SendAsync (It.IsAny<IMessage>()))
				.Returns(Task.Delay(0))
				.Callback<IMessage>(m =>  {
					sentUnsubscribe = m as Unsubscribe;
				});

			var formatter = new UnsubscribeFormatter (this.messageChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);

			await formatter.ReadAsync (packet);

			Assert.Equal (expectedUnsubscribe, sentUnsubscribe);
		}

		[Theory]
		[InlineData("Files/Unsubscribe_SingleTopic.json", "Files/Unsubscribe_SingleTopic.packet")]
		[InlineData("Files/Unsubscribe_MultiTopic.json", "Files/Unsubscribe_MultiTopic.packet")]
		public async Task when_writing_unsubscribe_packet_then_succeeds(string jsonPath, string packetPath)
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

			var formatter = new UnsubscribeFormatter (this.messageChannel.Object, this.byteChannel.Object);
			var unsubscribe = Packet.ReadMessage<Unsubscribe> (jsonPath);

			await formatter.WriteAsync (unsubscribe);

			Assert.Equal (expectedPacket, sentPacket);
		}
	}
}
