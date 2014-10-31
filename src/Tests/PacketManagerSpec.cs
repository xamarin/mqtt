using System;
using System.IO;
using System.Linq;
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
			var bytes = Packet.ReadAllBytes (packetPath);
			var jsonPath = Path.Combine (Environment.CurrentDirectory, "Files/Packets/Connect_Full.json");
			var packet = Packet.ReadPacket<Connect> (jsonPath);
			var formatter = new Mock<IFormatter> ();

			formatter.Setup(f => f.PacketType).Returns(PacketType.Connect);

			formatter
				.Setup (f => f.FormatAsync (It.Is<byte[]> (b => b.ToList().SequenceEqual(bytes))))
				.Returns (Task.FromResult<IPacket>(packet));

			var packetManager = new PacketManager (formatter.Object);
			var result =  await packetManager.GetAsync (bytes);

			Assert.Equal (packet, result);
		}

		[Fact]
		public async Task when_managing_packet_then_succeeds()
		{
			var jsonPath = Path.Combine (Environment.CurrentDirectory, "Files/Packets/Connect_Full.json");
			var packet = Packet.ReadPacket<Connect> (jsonPath);
			var packetPath = Path.Combine (Environment.CurrentDirectory, "Files/Binaries/Connect_Full.packet");
			var bytes = Packet.ReadAllBytes (packetPath);
			var formatter = new Mock<IFormatter> ();

			formatter.Setup(f => f.PacketType).Returns(PacketType.Connect);

			formatter
				.Setup (f => f.FormatAsync (It.Is<IPacket> (p => (Connect)p == packet)))
				.Returns (Task.FromResult(bytes));

			var packetManager = new PacketManager (formatter.Object);
			var result = await packetManager.GetAsync (packet);

			Assert.Equal (bytes, result);
		}

		[Fact]
		public void when_managing_unknown_packet_bytes_then_fails()
		{
			var connectPacketPath = Path.Combine (Environment.CurrentDirectory, "Files/Binaries/Connect_Full.packet");
			var connectPacket = Packet.ReadAllBytes (connectPacketPath);
			var formatter = new Mock<IFormatter> ();
			var packetManager = new PacketManager (formatter.Object);

			var ex = Assert.Throws<AggregateException> (() => packetManager.GetAsync (connectPacket).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}

		[Fact]
		public void when_managing_unknown_packet_then_fails()
		{
			var jsonPath = Path.Combine (Environment.CurrentDirectory, "Files/Packets/Connect_Full.json");
			var packet = Packet.ReadPacket<Connect> (jsonPath);
			var formatter = new Mock<IFormatter> ();
			var packetManager = new PacketManager (formatter.Object);

			var ex = Assert.Throws<AggregateException> (() => packetManager.GetAsync (packet).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}
	}
}
