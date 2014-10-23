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
		readonly Mock<IChannel<IMessage>> messageChannel;
		readonly Mock<IChannel<byte[]>> byteChannel;

		public ConnectAckFormatterSpec ()
		{
			this.messageChannel = new Mock<IChannel<IMessage>> ();
			this.byteChannel = new Mock<IChannel<byte[]>> ();
		}

		[Theory]
		[InlineData("Files/Packets/ConnectAck.packet", "Files/Messages/ConnectAck.json")]
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
		[InlineData("Files/Packets/ConnectAck_Invalid_HeaderFlag.packet")]
		[InlineData("Files/Packets/ConnectAck_Invalid_AckFlags.packet")]
		[InlineData("Files/Packets/ConnectAck_Invalid_SessionPresent.packet")]
		public void when_reading_invalid_connect_ack_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new ConnectAckFormatter (this.messageChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.ReadAsync (packet).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}

		[Theory]
		[InlineData("Files/Messages/ConnectAck.json", "Files/Packets/ConnectAck.packet")]
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

		[Theory]
		[InlineData("Files/Messages/ConnectAck_Invalid_SessionPresent.json")]
		public void when_writing_invalid_connect_ack_packet_then_fails(string jsonPath)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var formatter = new ConnectAckFormatter (this.messageChannel.Object, this.byteChannel.Object);
			var connectAck = Packet.ReadMessage<ConnectAck> (jsonPath);

			var ex = Assert.Throws<AggregateException> (() => formatter.WriteAsync (connectAck).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}
	}
}
