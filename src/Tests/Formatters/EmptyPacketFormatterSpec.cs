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
	public class EmptyPacketFormatterSpec
	{
		readonly Mock<IChannel<IPacket>> packetChannel;
		readonly Mock<IChannel<byte[]>> byteChannel;

		public EmptyPacketFormatterSpec ()
		{
			this.packetChannel = new Mock<IChannel<IPacket>> ();
			this.byteChannel = new Mock<IChannel<byte[]>> ();
		}
		
		[Theory]
		[InlineData("Files/Binaries/PingResponse.packet", PacketType.PingResponse, typeof(PingResponse))]
		[InlineData("Files/Binaries/PingRequest.packet", PacketType.PingRequest, typeof(PingRequest))]
		[InlineData("Files/Binaries/Disconnect.packet", PacketType.Disconnect, typeof(Disconnect))]
		public async Task when_reading_empty_packet_then_succeeds(string packetPath, PacketType packetType, Type type)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var sentPacket = default(IPacket);

			this.packetChannel
				.Setup (c => c.SendAsync (It.IsAny<IPacket>()))
				.Returns(Task.Delay(0))
				.Callback<IPacket>(m =>  {
					sentPacket = m;
				});

			var formatter = this.GetFormatter (packetType, type);
			var packet = Packet.ReadAllBytes (packetPath);

			await formatter.ReadAsync (packet);

			Assert.NotNull (sentPacket);
		}

		[Theory]
		[InlineData("Files/Binaries/PingResponse_Invalid_HeaderFlag.packet", PacketType.PingResponse, typeof(PingResponse))]
		[InlineData("Files/Binaries/PingRequest_Invalid_HeaderFlag.packet", PacketType.PingRequest, typeof(PingRequest))]
		[InlineData("Files/Binaries/Disconnect_Invalid_HeaderFlag.packet", PacketType.Disconnect, typeof(Disconnect))]
		public void when_reading_invalid_empty_packet_then_fails(string packetPath, PacketType packetType, Type type)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = this.GetFormatter (packetType, type);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.ReadAsync (packet).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}

		[Theory]
		[InlineData("Files/Binaries/PingResponse.packet", PacketType.PingResponse, typeof(PingResponse))]
		[InlineData("Files/Binaries/PingRequest.packet", PacketType.PingRequest, typeof(PingRequest))]
		[InlineData("Files/Binaries/Disconnect.packet", PacketType.Disconnect, typeof(Disconnect))]
		public async Task when_writing_empty_packet_then_succeeds(string packetPath, PacketType packetType, Type type)
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

			var formatter = this.GetFormatter (packetType, type);
			var packet = Activator.CreateInstance (type) as IPacket;

			await formatter.WriteAsync (packet);

			Assert.Equal (expectedPacket, sentPacket);
		}

		private IFormatter GetFormatter(PacketType packetType, Type type)
		{
			var genericType = typeof (EmptyPacketFormatter<>);
			var formatterType = genericType.MakeGenericType (type);
			var formatter = Activator.CreateInstance (formatterType, packetType, this.packetChannel.Object, this.byteChannel.Object) as IFormatter;

			return formatter;
		}
	}
}
