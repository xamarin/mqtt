using System;
using System.IO;
using System.Threading.Tasks;
using Hermes;
using Hermes.Formatters;
using Hermes.Packets;
using Moq;
using Xunit;

namespace Tests
{
	public class PacketManagerSpec
	{
		[Fact]
		public async Task when_managing_packet_bytes_then_succeeds()
		{
			var packetPath = Path.Combine (Environment.CurrentDirectory, "Files/Binaries/Connect_Full.packet");
			var packet = Packet.ReadAllBytes (packetPath);
			var formatter = new Mock<IFormatter> ();
			var sentPacket = default(byte[]);

			formatter.Setup(f => f.PacketType).Returns(PacketType.Connect);

			formatter
				.Setup (f => f.ReadAsync (It.IsAny<byte[]> ()))
				.Returns(Task.Delay(0))
				.Callback<byte[]> (b => {
					sentPacket = b;
				});

			var packetManager = new PacketManager (formatter.Object);

			await packetManager.ManageAsync (packet);

			Assert.Equal (packet, sentPacket);
		}

		[Fact]
		public async Task when_managing_packet_then_succeeds()
		{
			var jsonPath = Path.Combine (Environment.CurrentDirectory, "Files/Packets/Connect_Full.json");
			var packet = Packet.ReadPacket<Connect> (jsonPath);
			var formatter = new Mock<IFormatter> ();
			var sentPacket = default(Connect);

			formatter.Setup(f => f.PacketType).Returns(PacketType.Connect);

			formatter
				.Setup (f => f.WriteAsync (It.IsAny<IPacket> ()))
				.Returns(Task.Delay(0))
				.Callback<IPacket> (m => {
					sentPacket = m as Connect;
				});

			var packetManager = new PacketManager (formatter.Object);

			await packetManager.ManageAsync (packet);

			Assert.Equal (packet, sentPacket);
		}

		[Fact]
		public void when_managing_unknown_packet_bytes_then_fails()
		{
			var connectPacketPath = Path.Combine (Environment.CurrentDirectory, "Files/Binaries/Connect_Full.packet");
			var connectPacket = Packet.ReadAllBytes (connectPacketPath);
			var formatter = new Mock<IFormatter> ();
			var packetManager = new PacketManager (formatter.Object);

			var ex = Assert.Throws<AggregateException> (() => packetManager.ManageAsync (connectPacket).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}

		[Fact]
		public void when_managing_unknown_packet_then_fails()
		{
			var jsonPath = Path.Combine (Environment.CurrentDirectory, "Files/Packets/Connect_Full.json");
			var packet = Packet.ReadPacket<Connect> (jsonPath);
			var formatter = new Mock<IFormatter> ();
			var packetManager = new PacketManager (formatter.Object);

			var ex = Assert.Throws<AggregateException> (() => packetManager.ManageAsync (packet).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}
	}
}
