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
	public class PublishFormatterSpec
	{
		readonly Mock<IChannel<IPacket>> packetChannel;
		readonly Mock<IChannel<byte[]>> byteChannel;

		public PublishFormatterSpec ()
		{
			this.packetChannel = new Mock<IChannel<IPacket>> ();
			this.byteChannel = new Mock<IChannel<byte[]>> ();
		}
		
		[Theory]
		[InlineData("Files/Binaries/Publish_Full.packet", "Files/Packets/Publish_Full.json")]
		[InlineData("Files/Binaries/Publish_Min.packet", "Files/Packets/Publish_Min.json")]
		public async Task when_reading_publish_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedPublish = Packet.ReadPacket<Publish> (jsonPath);
			var sentPublish = default(Publish);

			this.packetChannel
				.Setup (c => c.SendAsync (It.IsAny<IPacket>()))
				.Returns(Task.Delay(0))
				.Callback<IPacket>(m =>  {
					sentPublish = m as Publish;
				});

			var formatter = new PublishFormatter (this.packetChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);

			await formatter.ReadAsync (packet);

			Assert.Equal (expectedPublish, sentPublish);
		}

		[Theory]
		[InlineData("Files/Binaries/Publish_Invalid_QualityOfService.packet")]
		[InlineData("Files/Binaries/Publish_Invalid_Duplicated.packet")]
		[InlineData("Files/Binaries/Publish_Invalid_Topic.packet")]
		public void when_reading_invalid_publish_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new PublishFormatter (this.packetChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.ReadAsync (packet).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}

		[Theory]
		[InlineData("Files/Packets/Publish_Full.json", "Files/Binaries/Publish_Full.packet")]
		[InlineData("Files/Packets/Publish_Min.json", "Files/Binaries/Publish_Min.packet")]
		public async Task when_writing_publish_packet_then_succeeds(string jsonPath, string packetPath)
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

			var formatter = new PublishFormatter (this.packetChannel.Object, this.byteChannel.Object);
			var publish = Packet.ReadPacket<Publish> (jsonPath);

			await formatter.WriteAsync (publish);

			Assert.Equal (expectedPacket, sentPacket);
		}

		[Theory]
		[InlineData("Files/Packets/Publish_Invalid_Duplicated.json")]
		[InlineData("Files/Packets/Publish_Invalid_Topic.json")]
		[InlineData("Files/Packets/Publish_Invalid_PacketId.json")]
		public void when_writing_invalid_publish_packet_then_fails(string jsonPath)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var formatter = new PublishFormatter (this.packetChannel.Object, this.byteChannel.Object);
			var publish = Packet.ReadPacket<Publish> (jsonPath);

			var ex = Assert.Throws<AggregateException> (() => formatter.WriteAsync (publish).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}
	}
}
