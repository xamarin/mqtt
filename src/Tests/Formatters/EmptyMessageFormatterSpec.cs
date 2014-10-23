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
	public class EmptyMessageFormatterSpec
	{
		readonly Mock<IChannel<IMessage>> messageChannel;
		readonly Mock<IChannel<byte[]>> byteChannel;

		public EmptyMessageFormatterSpec ()
		{
			this.messageChannel = new Mock<IChannel<IMessage>> ();
			this.byteChannel = new Mock<IChannel<byte[]>> ();
		}
		
		[Theory]
		[InlineData("Files/Packets/PingResponse.packet", MessageType.PingResponse, typeof(PingResponse))]
		[InlineData("Files/Packets/PingRequest.packet", MessageType.PingRequest, typeof(PingRequest))]
		[InlineData("Files/Packets/Disconnect.packet", MessageType.Disconnect, typeof(Disconnect))]
		public async Task when_reading_ping_response_packet_then_succeeds(string packetPath, MessageType messageType, Type type)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var sentMessage = default(IMessage);

			this.messageChannel
				.Setup (c => c.SendAsync (It.IsAny<IMessage>()))
				.Returns(Task.Delay(0))
				.Callback<IMessage>(m =>  {
					sentMessage = m;
				});

			var formatter = this.GetFormatter (messageType, type);
			var packet = Packet.ReadAllBytes (packetPath);

			await formatter.ReadAsync (packet);

			Assert.NotNull (sentMessage);
		}

		[Theory]
		[InlineData("Files/Packets/PingResponse_Invalid_HeaderFlag.packet", MessageType.PingResponse, typeof(PingResponse))]
		[InlineData("Files/Packets/PingRequest_Invalid_HeaderFlag.packet", MessageType.PingRequest, typeof(PingRequest))]
		[InlineData("Files/Packets/Disconnect_Invalid_HeaderFlag.packet", MessageType.Disconnect, typeof(Disconnect))]
		public void when_reading_invalid_ping_response_packet_then_fails(string packetPath, MessageType messageType, Type type)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = this.GetFormatter (messageType, type);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.ReadAsync (packet).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}

		[Theory]
		[InlineData("Files/Packets/PingResponse.packet", MessageType.PingResponse, typeof(PingResponse))]
		[InlineData("Files/Packets/PingRequest.packet", MessageType.PingRequest, typeof(PingRequest))]
		[InlineData("Files/Packets/Disconnect.packet", MessageType.Disconnect, typeof(Disconnect))]
		public async Task when_writing_ping_response_packet_then_succeeds(string packetPath, MessageType messageType, Type type)
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

			var formatter = this.GetFormatter (messageType, type);
			var message = Activator.CreateInstance (type) as IMessage;

			await formatter.WriteAsync (message);

			Assert.Equal (expectedPacket, sentPacket);
		}

		private IFormatter GetFormatter(MessageType messageType, Type type)
		{
			var genericType = typeof (EmptyMessageFormatter<>);
			var formatterType = genericType.MakeGenericType (type);
			var formatter = Activator.CreateInstance (formatterType, messageType, this.messageChannel.Object, this.byteChannel.Object) as IFormatter;

			return formatter;
		}
	}
}
