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
		readonly Mock<IChannel<IMessage>> messageChannel;
		readonly Mock<IChannel<byte[]>> byteChannel;

		public SubscribeFormatterSpec ()
		{
			this.messageChannel = new Mock<IChannel<IMessage>> ();
			this.byteChannel = new Mock<IChannel<byte[]>> ();
		}
		
		[Theory]
		[InlineData("Files/Packets/Subscribe_SingleTopic.packet", "Files/Messages/Subscribe_SingleTopic.json")]
		[InlineData("Files/Packets/Subscribe_MultiTopic.packet", "Files/Messages/Subscribe_MultiTopic.json")]
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
		[InlineData("Files/Packets/Subscribe_Invalid_HeaderFlag.packet")]
		public void when_reading_invalid_subscribe_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new SubscribeFormatter (this.messageChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.ReadAsync (packet).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}

		[Theory]
		[InlineData("Files/Packets/Subscribe_Invalid_TopicFilterQosPair.packet")]
		[InlineData("Files/Packets/Subscribe_Invalid_TopicFilterQosPair2.packet")]
		[InlineData("Files/Packets/Subscribe_Invalid_TopicFilterQos.packet")]
		public void when_reading_invalid_topic_filter_in_subscribe_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new SubscribeFormatter (this.messageChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.ReadAsync (packet).Wait());

			Assert.True (ex.InnerException is ViolationProtocolException);
		}

		[Theory]
		[InlineData("Files/Messages/Subscribe_SingleTopic.json", "Files/Packets/Subscribe_SingleTopic.packet")]
		[InlineData("Files/Messages/Subscribe_MultiTopic.json", "Files/Packets/Subscribe_MultiTopic.packet")]
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

		[Theory]
		[InlineData("Files/Messages/Subscribe_Invalid_EmptyTopicFilters.json")]
		public void when_writing_invalid_subscribe_packet_then_fails(string jsonPath)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var formatter = new SubscribeFormatter (this.messageChannel.Object, this.byteChannel.Object);
			var subscribe = Packet.ReadMessage<Subscribe> (jsonPath);

			var ex = Assert.Throws<AggregateException> (() => formatter.WriteAsync (subscribe).Wait());

			Assert.True (ex.InnerException is ViolationProtocolException);
		}
	}
}
