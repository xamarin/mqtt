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
	public class PublishReleaseFormatterSpec
	{
		readonly Mock<IChannel<IPacket>> packetChannel;
		readonly Mock<IChannel<byte[]>> byteChannel;

		public PublishReleaseFormatterSpec ()
		{
			this.packetChannel = new Mock<IChannel<IPacket>> ();
			this.byteChannel = new Mock<IChannel<byte[]>> ();
		}

		[Theory]
		[InlineData("Files/Binaries/PublishRelease.packet", "Files/Packets/PublishRelease.json")]
		public async Task when_reading_publish_release_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedPublishRelease = Packet.ReadPacket<PublishRelease> (jsonPath);
			var sentPublishRelease = default(PublishRelease);

			this.packetChannel
				.Setup (c => c.SendAsync (It.IsAny<IPacket>()))
				.Returns(Task.Delay(0))
				.Callback<IPacket>(m =>  {
					sentPublishRelease = m as PublishRelease;
				});

			var formatter = new FlowPacketFormatter<PublishRelease>(PacketType.PublishRelease, id => new PublishRelease(id), this.packetChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);

			await formatter.ReadAsync (packet);

			Assert.Equal (expectedPublishRelease, sentPublishRelease);
		}

		[Theory]
		[InlineData("Files/Binaries/PublishRelease_Invalid_HeaderFlag.packet")]
		public void when_reading_invalid_publish_release_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new FlowPacketFormatter<PublishRelease> (PacketType.PublishRelease, id => new PublishRelease(id), this.packetChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.ReadAsync (packet).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}

		[Theory]
		[InlineData("Files/Packets/PublishRelease.json", "Files/Binaries/PublishRelease.packet")]
		public async Task when_writing_publish_release_packet_then_succeeds(string jsonPath, string packetPath)
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

			var formatter = new FlowPacketFormatter<PublishRelease>(PacketType.PublishRelease, id => new PublishRelease(id), this.packetChannel.Object, this.byteChannel.Object);
			var publishRelease = Packet.ReadPacket<PublishRelease> (jsonPath);

			await formatter.WriteAsync (publishRelease);

			Assert.Equal (expectedPacket, sentPacket);
		}
	}
}
