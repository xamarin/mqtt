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

			var formatter = this.GetFormatter (packetType, type);
			var packet = Packet.ReadAllBytes (packetPath);

			var result = await formatter.FormatAsync (packet);

			Assert.NotNull (result);
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
			
			var ex = Assert.Throws<AggregateException> (() => formatter.FormatAsync (packet).Wait());

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
			var formatter = this.GetFormatter (packetType, type);
			var packet = Activator.CreateInstance (type) as IPacket;

			var result = await formatter.FormatAsync (packet);

			Assert.Equal (expectedPacket, result);
		}

		private IFormatter GetFormatter(PacketType packetType, Type type)
		{
			var genericType = typeof (EmptyPacketFormatter<>);
			var formatterType = genericType.MakeGenericType (type);
			var formatter = Activator.CreateInstance (formatterType, packetType) as IFormatter;

			return formatter;
		}
	}
}
