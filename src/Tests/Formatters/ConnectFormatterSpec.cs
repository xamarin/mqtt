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
	public class ConnectFormatterSpec
	{
		readonly Mock<IChannel<IPacket>> packetChannel;
		readonly Mock<IChannel<byte[]>> byteChannel;

		public ConnectFormatterSpec ()
		{
			this.packetChannel = new Mock<IChannel<IPacket>> ();
			this.byteChannel = new Mock<IChannel<byte[]>> ();
		}
		
		[Theory]
		[InlineData("Files/Binaries/Connect_Full.packet", "Files/Packets/Connect_Full.json")]
		[InlineData("Files/Binaries/Connect_Min.packet", "Files/Packets/Connect_Min.json")]
		public async Task when_reading_connect_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedConnect = Packet.ReadPacket<Connect> (jsonPath);
			var sentConnect = default(Connect);

			this.packetChannel
				.Setup (c => c.SendAsync (It.IsAny<IPacket>()))
				.Returns(Task.Delay(0))
				.Callback<IPacket>(m =>  {
					sentConnect = m as Connect;
				});

			var formatter = new ConnectFormatter (this.packetChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);

			await formatter.ReadAsync (packet);

			Assert.Equal (expectedConnect, sentConnect);
		}

		[Theory]
		[InlineData("Files/Binaries/Connect_Invalid_HeaderFlag.packet")]
		[InlineData("Files/Binaries/Connect_Invalid_ProtocolName.packet")]
		[InlineData("Files/Binaries/Connect_Invalid_ConnectReservedFlag.packet")]
		[InlineData("Files/Binaries/Connect_Invalid_QualityOfService.packet")]
		[InlineData("Files/Binaries/Connect_Invalid_WillFlags.packet")]
		[InlineData("Files/Binaries/Connect_Invalid_UserNamePassword.packet")]
		[InlineData("Files/Binaries/Connect_Invalid_ProtocolLevel.packet")]
		public void when_reading_invalid_connect_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new ConnectFormatter (this.packetChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.ReadAsync (packet).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}

		[Theory]
		[InlineData("Files/Binaries/Connect_Invalid_ClientIdEmpty.packet")]
		[InlineData("Files/Binaries/Connect_Invalid_ClientIdBadFormat.packet")]
		[InlineData("Files/Binaries/Connect_Invalid_ClientIdInvalidLength.packet")]
		public void when_reading_invalid_client_id_in_connect_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new ConnectFormatter (this.packetChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);

			var ex = Assert.Throws<AggregateException> (() => formatter.ReadAsync (packet).Wait());

			Assert.True (ex.InnerException is ConnectProtocolException);
		}

		[Theory]
		[InlineData("Files/Packets/Connect_Full.json", "Files/Binaries/Connect_Full.packet")]
		[InlineData("Files/Packets/Connect_Min.json", "Files/Binaries/Connect_Min.packet")]
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

			var formatter = new ConnectFormatter (this.packetChannel.Object, this.byteChannel.Object);
			var connect = Packet.ReadPacket<Connect> (jsonPath);

			await formatter.WriteAsync (connect);

			Assert.Equal (expectedPacket, sentPacket);
		}

		[Theory]
		[InlineData("Files/Packets/Connect_Invalid_UserNamePassword.json")]
		[InlineData("Files/Packets/Connect_Invalid_ClientIdEmpty.json")]
		[InlineData("Files/Packets/Connect_Invalid_ClientIdBadFormat.json")]
		[InlineData("Files/Packets/Connect_Invalid_ClientIdInvalidLength.json")]
		public void when_writing_invalid_connect_packet_then_fails(string jsonPath)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var formatter = new ConnectFormatter (this.packetChannel.Object, this.byteChannel.Object);
			var connect = Packet.ReadPacket<Connect> (jsonPath);

			var ex = Assert.Throws<AggregateException> (() => formatter.WriteAsync (connect).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}
	}
}
