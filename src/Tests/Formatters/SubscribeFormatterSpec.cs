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
	public class SubscribeFormatterSpec
	{
		private readonly Mock<IChannel<IMessage>> messageChannel;
		private readonly Mock<IChannel<byte[]>> byteChannel;

		public SubscribeFormatterSpec ()
		{
			this.messageChannel = new Mock<IChannel<IMessage>> ();
			this.byteChannel = new Mock<IChannel<byte[]>> ();
		}
		
		[Theory]
		[InlineData("Files/Subscribe_SingleTopic.packet", "Files/Subscribe_SingleTopic.json")]
		[InlineData("Files/Subscribe_MultiTopic.packet", "Files/Subscribe_MultiTopic.json")]
		public async Task when_reading_subscribe_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedSubscribe = Packet.ReadMessage<Subscribe> (jsonPath);
			var sentSubscribe = default(Subscribe);

			this.messageChannel
				.Setup (c => c.SendAsync (It.IsAny<IMessage>()))
				.Returns(Task.Delay(0))
				.Callback<IMessage>(m =>  {
					sentSubscribe = m as Subscribe;
				});

			var formatter = new SubscribeFormatter (this.messageChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);

			await formatter.ReadAsync (packet);

			Assert.Equal (expectedSubscribe, sentSubscribe);
		}

		[Theory]
		[InlineData("Files/Subscribe_SingleTopic.json", "Files/Subscribe_SingleTopic.packet")]
		[InlineData("Files/Subscribe_MultiTopic.json", "Files/Subscribe_MultiTopic.packet")]
		public async Task when_writing_subscribe_packet_then_succeeds(string jsonPath, string packetPath)
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

			var formatter = new SubscribeFormatter (this.messageChannel.Object, this.byteChannel.Object);
			var subscribe = Packet.ReadMessage<Subscribe> (jsonPath);

			await formatter.WriteAsync (subscribe);

			Assert.Equal (expectedPacket, sentPacket);
		}
	}
}
