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
	public class PublishAckFormatterSpec
	{
		readonly Mock<IChannel<IPacket>> packetChannel;
		readonly Mock<IChannel<byte[]>> byteChannel;

		public PublishAckFormatterSpec ()
		{
			this.packetChannel = new Mock<IChannel<IPacket>> ();
			this.byteChannel = new Mock<IChannel<byte[]>> ();
		}

		[Theory]
		[InlineData("Files/Binaries/PublishAck.packet", "Files/Packets/PublishAck.json")]
		public async Task when_reading_publish_ack_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedPublishAck = Packet.ReadPacket<PublishAck> (jsonPath);
			var sentPublishAck = default(PublishAck);

			this.packetChannel
				.Setup (c => c.SendAsync (It.IsAny<IPacket>()))
				.Returns(Task.Delay(0))
				.Callback<IPacket>(m =>  {
					sentPublishAck = m as PublishAck;
				});

			var formatter = new FlowPacketFormatter<PublishAck>(PacketType.PublishAck, id => new PublishAck(id), this.packetChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);

			await formatter.ReadAsync (packet);

			Assert.Equal (expectedPublishAck, sentPublishAck);
		}

		[Theory]
		[InlineData("Files/Binaries/PublishAck_Invalid_HeaderFlag.packet")]
		public void when_reading_invalid_publish_ack_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new FlowPacketFormatter<PublishAck> (PacketType.PublishAck, id => new PublishAck(id), this.packetChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.ReadAsync (packet).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}

		[Theory]
		[InlineData("Files/Packets/PublishAck.json", "Files/Binaries/PublishAck.packet")]
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

			var formatter = new FlowPacketFormatter<PublishAck>(PacketType.PublishAck, id => new PublishAck(id), this.packetChannel.Object, this.byteChannel.Object);
			var publishAck = Packet.ReadPacket<PublishAck> (jsonPath);

			await formatter.WriteAsync (publishAck);

			Assert.Equal (expectedPacket, sentPacket);
		}
	}
}
