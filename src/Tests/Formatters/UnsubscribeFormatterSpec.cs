using System;
using System.IO;
using System.Threading.Tasks;
using Hermes;
using Hermes.Formatters;
using Hermes.Packets;
using Moq;
using Xunit;
using Xunit.Extensions;

namespace Tests.Formatters
{
	public class UnsubscribeFormatterSpec
	{
		readonly Mock<IChannel<IPacket>> packetChannel;
		readonly Mock<IChannel<byte[]>> byteChannel;

		public UnsubscribeFormatterSpec ()
		{
			this.packetChannel = new Mock<IChannel<IPacket>> ();
			this.byteChannel = new Mock<IChannel<byte[]>> ();
		}
		
		[Theory]
		[InlineData("Files/Binaries/Unsubscribe_SingleTopic.packet", "Files/Packets/Unsubscribe_SingleTopic.json")]
		[InlineData("Files/Binaries/Unsubscribe_MultiTopic.packet", "Files/Packets/Unsubscribe_MultiTopic.json")]
		public async Task when_reading_unsubscribe_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedUnsubscribe = Packet.ReadPacket<Unsubscribe> (jsonPath);
			var sentUnsubscribe = default(Unsubscribe);

			this.packetChannel
				.Setup (c => c.SendAsync (It.IsAny<IPacket>()))
				.Returns(Task.Delay(0))
				.Callback<IPacket>(m =>  {
					sentUnsubscribe = m as Unsubscribe;
				});

			var formatter = new UnsubscribeFormatter (this.packetChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);

			await formatter.ReadAsync (packet);

			Assert.Equal (expectedUnsubscribe, sentUnsubscribe);
		}

		[Theory]
		[InlineData("Files/Binaries/Unsubscribe_Invalid_HeaderFlag.packet")]
		public void when_reading_invalid_unsubscribe_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new UnsubscribeFormatter (this.packetChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.ReadAsync (packet).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}

		[Theory]
		[InlineData("Files/Binaries/Unsubscribe_Invalid_EmptyTopics.packet")]
		public void when_reading_invalid_topic_in_unsubscribe_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new UnsubscribeFormatter (this.packetChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.ReadAsync (packet).Wait());

			Assert.True (ex.InnerException is ViolationProtocolException);
		}

		[Theory]
		[InlineData("Files/Packets/Unsubscribe_SingleTopic.json", "Files/Binaries/Unsubscribe_SingleTopic.packet")]
		[InlineData("Files/Packets/Unsubscribe_MultiTopic.json", "Files/Binaries/Unsubscribe_MultiTopic.packet")]
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

			var formatter = new UnsubscribeFormatter (this.packetChannel.Object, this.byteChannel.Object);
			var unsubscribe = Packet.ReadPacket<Unsubscribe> (jsonPath);

			await formatter.WriteAsync (unsubscribe);

			Assert.Equal (expectedPacket, sentPacket);
		}

		[Theory]
		[InlineData("Files/Packets/Unsubscribe_Invalid_EmptyTopics.json")]
		public void when_writing_invalid_unsubscribe_packet_then_fails(string jsonPath)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var formatter = new UnsubscribeFormatter (this.packetChannel.Object, this.byteChannel.Object);
			var unsubscribe = Packet.ReadPacket<Unsubscribe> (jsonPath);

			var ex = Assert.Throws<AggregateException> (() => formatter.WriteAsync (unsubscribe).Wait());

			Assert.True (ex.InnerException is ViolationProtocolException);
		}
	}
}
