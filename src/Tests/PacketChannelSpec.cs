using System;
using System.IO;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Hermes;
using Hermes.Packets;
using Moq;
using Xunit;

namespace Tests
{
	public class PacketChannelSpec
	{
		[Fact]
		public void when_creating_packet_channel_then_succeeds()
		{
			var receiver = new Subject<byte>();
			var bufferedChannel = new Mock<IBufferedChannel<byte>> ();

			bufferedChannel.Setup (x => x.Receiver).Returns (receiver);

			var factory = new PacketChannelFactory ();
			var channel = factory.CreateChannel (bufferedChannel.Object);

			Assert.NotNull (channel);
		}

		[Fact]
		public void when_reading_bytes_then_notifies_packet()
		{
			var receiver = new Subject<byte[]> ();
			var innerChannel = new Mock<IChannel<byte[]>>();

			innerChannel.Setup (x => x.Receiver).Returns (receiver);

			var jsonPath = Path.Combine (Environment.CurrentDirectory, "Files/Packets/Connect_Full.json");
			var expectedPacket = Packet.ReadPacket<Connect> (jsonPath);

			var manager = new Mock<IPacketManager> ();

			manager.Setup(x => x.GetAsync(It.IsAny<byte[]>()))
				.Returns(Task.FromResult<IPacket>(expectedPacket));

			var channel = new PacketChannel (innerChannel.Object, manager.Object);

			var receivedPacket = default (IPacket);

			channel.Receiver.Subscribe (packet => {
				receivedPacket = packet;
			});

			var packetPath = Path.Combine (Environment.CurrentDirectory, "Files/Binaries/Connect_Full.packet");
			var readPacket = Packet.ReadAllBytes (packetPath);

			receiver.OnNext (readPacket);

			Assert.NotNull (receivedPacket);
			Assert.Equal (expectedPacket, receivedPacket);
		}
	}
}
