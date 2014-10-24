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
	public class UnsubscribeAckFormatterSpec
	{
		readonly Mock<IChannel<IPacket>> packetChannel;
		readonly Mock<IChannel<byte[]>> byteChannel;

		public UnsubscribeAckFormatterSpec ()
		{
			this.packetChannel = new Mock<IChannel<IPacket>> ();
			this.byteChannel = new Mock<IChannel<byte[]>> ();
		}

		[Theory]
		[InlineData("Files/Binaries/UnsubscribeAck.packet", "Files/Packets/UnsubscribeAck.json")]
		public async Task when_reading_unsubscribe_ack_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedUnsubscribeAck = Packet.ReadPacket<UnsubscribeAck> (jsonPath);
			var sentUnsubscribeAck = default(UnsubscribeAck);

			this.packetChannel
				.Setup (c => c.SendAsync (It.IsAny<IPacket>()))
				.Returns(Task.Delay(0))
				.Callback<IPacket>(m =>  {
					sentUnsubscribeAck = m as UnsubscribeAck;
				});

			var formatter = new FlowPacketFormatter<UnsubscribeAck>(PacketType.UnsubscribeAck, id => new UnsubscribeAck(id), this.packetChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);

			await formatter.ReadAsync (packet);

			Assert.Equal (expectedUnsubscribeAck, sentUnsubscribeAck);
		}

		[Theory]
		[InlineData("Files/Binaries/UnsubscribeAck_Invalid_HeaderFlag.packet")]
		public void when_reading_invalid_unsubscribe_ack_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new FlowPacketFormatter<UnsubscribeAck> (PacketType.UnsubscribeAck, id => new UnsubscribeAck(id), this.packetChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.ReadAsync (packet).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}

		[Theory]
		[InlineData("Files/Packets/UnsubscribeAck.json", "Files/Binaries/UnsubscribeAck.packet")]
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

			var formatter = new FlowPacketFormatter<UnsubscribeAck>(PacketType.UnsubscribeAck, id => new UnsubscribeAck(id), this.packetChannel.Object, this.byteChannel.Object);
			var unsubscribeAck = Packet.ReadPacket<UnsubscribeAck> (jsonPath);

			await formatter.WriteAsync (unsubscribeAck);

			Assert.Equal (expectedPacket, sentPacket);
		}
	}
}
