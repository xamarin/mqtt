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
	public class SubscribeAckFormatterSpec
	{
		readonly Mock<IChannel<IPacket>> packetChannel;
		readonly Mock<IChannel<byte[]>> byteChannel;

		public SubscribeAckFormatterSpec ()
		{
			this.packetChannel = new Mock<IChannel<IPacket>> ();
			this.byteChannel = new Mock<IChannel<byte[]>> ();
		}
		
		[Theory]
		[InlineData("Files/Binaries/SubscribeAck_SingleTopic.packet", "Files/Packets/SubscribeAck_SingleTopic.json")]
		[InlineData("Files/Binaries/SubscribeAck_MultiTopic.packet", "Files/Packets/SubscribeAck_MultiTopic.json")]
		public async Task when_reading_subscribe_ack_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedSubscribeAck = Packet.ReadPacket<SubscribeAck> (jsonPath);
			var sentSubscribeAck = default(SubscribeAck);

			this.packetChannel
				.Setup (c => c.SendAsync (It.IsAny<IPacket>()))
				.Returns(Task.Delay(0))
				.Callback<IPacket>(m =>  {
					sentSubscribeAck = m as SubscribeAck;
				});

			var formatter = new SubscribeAckFormatter (this.packetChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);

			await formatter.ReadAsync (packet);

			Assert.Equal (expectedSubscribeAck, sentSubscribeAck);
		}

		[Theory]
		[InlineData("Files/Binaries/SubscribeAck_Invalid_HeaderFlag.packet")]
		public void when_reading_invalid_subscribe_ack_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new SubscribeAckFormatter (this.packetChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.ReadAsync (packet).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}

		[Theory]
		[InlineData("Files/Binaries/SubscribeAck_Invalid_EmptyReturnCodes.packet")]
		[InlineData("Files/Binaries/SubscribeAck_Invalid_ReturnCodes.packet")]
		public void when_reading_invalid_return_code_in_subscribe_ack_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new SubscribeAckFormatter (this.packetChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.ReadAsync (packet).Wait());

			Assert.True (ex.InnerException is ViolationProtocolException);
		}

		[Theory]
		[InlineData("Files/Packets/SubscribeAck_SingleTopic.json", "Files/Binaries/SubscribeAck_SingleTopic.packet")]
		[InlineData("Files/Packets/SubscribeAck_MultiTopic.json", "Files/Binaries/SubscribeAck_MultiTopic.packet")]
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

			var formatter = new SubscribeAckFormatter (this.packetChannel.Object, this.byteChannel.Object);
			var subscribeAck = Packet.ReadPacket<SubscribeAck> (jsonPath);

			await formatter.WriteAsync (subscribeAck);

			Assert.Equal (expectedPacket, sentPacket);
		}

		[Theory]
		[InlineData("Files/Packets/SubscribeAck_Invalid_EmptyReturnCodes.json")]
		public void when_writing_invalid_subscribe_ack_packet_then_fails(string jsonPath)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var formatter = new SubscribeAckFormatter (this.packetChannel.Object, this.byteChannel.Object);
			var subscribeAck = Packet.ReadPacket<SubscribeAck> (jsonPath);

			var ex = Assert.Throws<AggregateException> (() => formatter.WriteAsync (subscribeAck).Wait());

			Assert.True (ex.InnerException is ViolationProtocolException);
		}
	}
}
