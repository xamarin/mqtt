using System;
using System.IO;
using System.Reactive.Subjects;
using Hermes;
using Moq;
using Xunit;

namespace Tests
{
	public class BinaryChannelSpec
	{
		[Fact]
		public void when_reading_complete_packet_then_notifies()
		{
			var socket = new Mock<IBufferedChannel<byte>>();
			var receiver = new Subject<byte>();

			socket.Setup(s => s.Receiver).Returns(receiver);

			var channel = new BinaryChannel (socket.Object);
			var receivedPacket = default(byte[]);

			channel.Receiver.Subscribe(packet => {
				receivedPacket = packet;
			});

			var packetPath = Path.Combine (Environment.CurrentDirectory, "Files/Binaries/Connect_Full.packet");
			var readPacket = Packet.ReadAllBytes (packetPath);

			foreach (var @byte in readPacket) {
				receiver.OnNext (@byte);
			}

			Assert.Equal (readPacket, receivedPacket);
		}

		[Fact]
		public void when_reading_incomplete_packet_then_does_not_notify()
		{
			var socket = new Mock<IBufferedChannel<byte>>();
			var receiver = new Subject<byte>();

			socket.Setup(s => s.Receiver).Returns(receiver);

			var channel = new BinaryChannel (socket.Object);
			var receivedPacket = default(byte[]);

			channel.Receiver.Subscribe(packet => {
				receivedPacket = packet;
			});

			var packetPath = Path.Combine (Environment.CurrentDirectory, "Files/Binaries/Connect_Full.packet");
			var readPacket = Packet.ReadAllBytes (packetPath);

			readPacket = readPacket.Bytes (0, readPacket.Length - 2);

			foreach (var @byte in readPacket) {
				receiver.OnNext (@byte);
			}

			Assert.Null (receivedPacket);
		}
	}
}
