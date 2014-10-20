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
	public class ConnectFormatterSpec
	{
		private readonly Mock<IChannel<IMessage>> messageChannel;
		private readonly Mock<IChannel<byte[]>> byteChannel;

		public ConnectFormatterSpec ()
		{
			this.messageChannel = new Mock<IChannel<IMessage>> ();
			this.byteChannel = new Mock<IChannel<byte[]>> ();
		}
		
		[Theory]
		[InlineData("Files/Connect_Full.packet", "Files/Connect_Full.json")]
		[InlineData("Files/Connect_Min.packet", "Files/Connect_Min.json")]
		public async Task when_reading_connect_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedConnect = Packet.ReadMessage<Connect> (jsonPath);
			var sentConnect = default(Connect);

			this.messageChannel
				.Setup (c => c.SendAsync (It.IsAny<IMessage>()))
				.Returns(Task.Delay(0))
				.Callback<IMessage>(m =>  {
					sentConnect = m as Connect;
				});

			var formatter = new ConnectFormatter (this.messageChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);

			await formatter.ReadAsync (packet);

			Assert.Equal (expectedConnect, sentConnect);
		}

		[Theory]
		[InlineData("Files/Connect_Full.json", "Files/Connect_Full.packet")]
		[InlineData("Files/Connect_Min.json", "Files/Connect_Min.packet")]
		public async Task when_writing_connect_packet_then_succeeds(string jsonPath, string packetPath)
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

			var formatter = new ConnectFormatter (this.messageChannel.Object, this.byteChannel.Object);
			var connect = Packet.ReadMessage<Connect> (jsonPath);

			await formatter.WriteAsync (connect);

			Assert.Equal (expectedPacket, sentPacket);
		}
	}
}
