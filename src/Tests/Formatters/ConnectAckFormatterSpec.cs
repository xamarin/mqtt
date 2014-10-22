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
	public class ConnectAckFormatterSpec
	{
		private readonly Mock<IChannel<IMessage>> messageChannel;
		private readonly Mock<IChannel<byte[]>> byteChannel;

		public ConnectAckFormatterSpec ()
		{
			this.messageChannel = new Mock<IChannel<IMessage>> ();
			this.byteChannel = new Mock<IChannel<byte[]>> ();
		}

		[Theory]
		[InlineData("Files/ConnectAck.packet", "Files/ConnectAck.json")]
		public async Task when_reading_connect_ack_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedConnectAck = Packet.ReadMessage<ConnectAck> (jsonPath);
			var sentConnectAck = default(ConnectAck);

			this.messageChannel
				.Setup (c => c.SendAsync (It.IsAny<IMessage>()))
				.Returns(Task.Delay(0))
				.Callback<IMessage>(m =>  {
					sentConnectAck = m as ConnectAck;
				});

			var formatter = new ConnectAckFormatter (this.messageChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);

			await formatter.ReadAsync (packet);

			Assert.Equal (expectedConnectAck, sentConnectAck);
		}

		[Theory]
		[InlineData("Files/ConnectAck.json", "Files/ConnectAck.packet")]
		public async Task when_writing_connect_ack_packet_then_succeeds(string jsonPath, string packetPath)
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

			var formatter = new ConnectAckFormatter (this.messageChannel.Object, this.byteChannel.Object);
			var connectAck = Packet.ReadMessage<ConnectAck> (jsonPath);

			await formatter.WriteAsync (connectAck);

			Assert.Equal (expectedPacket, sentPacket);
		}
	}
}
