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
	public class DisconnectFormatterSpec
	{
		private readonly Mock<IChannel<IMessage>> messageChannel;
		private readonly Mock<IChannel<byte[]>> byteChannel;

		public DisconnectFormatterSpec ()
		{
			this.messageChannel = new Mock<IChannel<IMessage>> ();
			this.byteChannel = new Mock<IChannel<byte[]>> ();
		}
		
		[Theory]
		[InlineData("Files/Packets/Disconnect.packet")]
		public async Task when_reading_disconnect_packet_then_succeeds(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var sentDisconnect = default(Disconnect);

			this.messageChannel
				.Setup (c => c.SendAsync (It.IsAny<IMessage>()))
				.Returns(Task.Delay(0))
				.Callback<IMessage>(m =>  {
					sentDisconnect = m as Disconnect;
				});

			var formatter = new EmptyMessageFormatter<Disconnect> (MessageType.Disconnect, this.messageChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);

			await formatter.ReadAsync (packet);

			Assert.NotNull (sentDisconnect);
		}

		[Theory]
		[InlineData("Files/Packets/Disconnect_Invalid_HeaderFlag.packet")]
		public void when_reading_invalid_disconnect_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new EmptyMessageFormatter<Disconnect> (MessageType.Disconnect, this.messageChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.ReadAsync (packet).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}

		[Theory]
		[InlineData("Files/Packets/Disconnect.packet")]
		public async Task when_writing_disconnect_packet_then_succeeds(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var expectedPacket = Packet.ReadAllBytes (packetPath);
			var sentPacket = default(byte[]);

			this.byteChannel
				.Setup (c => c.SendAsync (It.IsAny<byte[]>()))
				.Returns(Task.Delay(0))
				.Callback<byte[]>(b =>  {
					sentPacket = b;
				});

			var formatter = new EmptyMessageFormatter<Disconnect> (MessageType.Disconnect, this.messageChannel.Object, this.byteChannel.Object);
			var disconnect = new Disconnect ();

			await formatter.WriteAsync (disconnect);

			Assert.Equal (expectedPacket, sentPacket);
		}
	}
}
