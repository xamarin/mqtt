using System;
using System.IO;
using System.Threading.Tasks;
using Hermes;
using Hermes.Formatters;
using Hermes.Messages;
using Moq;
using Xunit;

namespace Tests
{
	public class MessageManagerSpec
	{
		[Fact]
		public async Task when_managing_packet_then_succeeds()
		{
			var packetPath = Path.Combine (Environment.CurrentDirectory, "Files/Connect_Full.packet");
			var packet = Packet.ReadAllBytes (packetPath);
			var formatter = new Mock<IFormatter> ();
			var sentPacket = default(byte[]);

			formatter.Setup(f => f.MessageType).Returns(MessageType.Connect);

			formatter
				.Setup (f => f.ReadAsync (It.IsAny<byte[]> ()))
				.Returns(Task.Delay(0))
				.Callback<byte[]> (b => {
					sentPacket = b;
				});

			var messageManager = new MessageManager (formatter.Object);

			await messageManager.ManageAsync (packet);

			Assert.Equal (packet, sentPacket);
		}

		[Fact]
		public async Task when_managing_message_then_succeeds()
		{
			var jsonPath = Path.Combine (Environment.CurrentDirectory, "Files/Connect_Full.json");
			var message = Packet.ReadMessage<Connect> (jsonPath);
			var formatter = new Mock<IFormatter> ();
			var sentMessage = default(Connect);

			formatter.Setup(f => f.MessageType).Returns(MessageType.Connect);

			formatter
				.Setup (f => f.WriteAsync (It.IsAny<IMessage> ()))
				.Returns(Task.Delay(0))
				.Callback<IMessage> (m => {
					sentMessage = m as Connect;
				});

			var messageManager = new MessageManager (formatter.Object);

			await messageManager.ManageAsync (message);

			Assert.Equal (message, sentMessage);
		}

		[Fact]
		public async Task when_managing_unknown_packet_then_fails()
		{
			var connectPacketPath = Path.Combine (Environment.CurrentDirectory, "Files/Connect_Full.packet");
			var connectPacket = Packet.ReadAllBytes (connectPacketPath);
			var formatter = new Mock<IFormatter> ();
			var messageManager = new MessageManager (formatter.Object);

			try {
				 await messageManager.ManageAsync (connectPacket);

				 Assert.True (false, "Test Error: Message Manager has no formatter registered for Connect packet, hence it should have failed");
			} catch (Exception ex) {
				Assert.True (ex is ProtocolException);
			}
		}

		[Fact]
		public async Task when_managing_unknown_message_then_fails()
		{
			var jsonPath = Path.Combine (Environment.CurrentDirectory, "Files/Connect_Full.json");
			var message = Packet.ReadMessage<Connect> (jsonPath);
			var formatter = new Mock<IFormatter> ();
			var messageManager = new MessageManager (formatter.Object);

			try {
				 await messageManager.ManageAsync (message);

				 Assert.True (false, "Test Error: Message Manager has no formatter registered for Connect message, hence it should have failed");
			} catch (Exception ex) {
				Assert.True (ex is ProtocolException);
			}
		}
	}
}
