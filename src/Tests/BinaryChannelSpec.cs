using System;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Hermes;
using Moq;
using Xunit;
using Xunit.Extensions;

namespace Tests
{
	public class BinaryChannelSpec
	{
		[Theory]
		[InlineData("Files/Binaries/Connect_Full.packet")]
		[InlineData("Files/Binaries/Connect_Min.packet")]
		[InlineData("Files/Binaries/ConnectAck.packet")]
		[InlineData("Files/Binaries/Disconnect.packet")]
		[InlineData("Files/Binaries/PingRequest.packet")]
		[InlineData("Files/Binaries/PingResponse.packet")]
		[InlineData("Files/Binaries/Publish_Full.packet")]
		[InlineData("Files/Binaries/Publish_Min.packet")]
		[InlineData("Files/Binaries/PublishAck.packet")]
		[InlineData("Files/Binaries/PublishComplete.packet")]
		[InlineData("Files/Binaries/PublishReceived.packet")]
		[InlineData("Files/Binaries/PublishRelease.packet")]
		[InlineData("Files/Binaries/Subscribe_MultiTopic.packet")]
		[InlineData("Files/Binaries/Subscribe_SingleTopic.packet")]
		[InlineData("Files/Binaries/SubscribeAck_MultiTopic.packet")]
		[InlineData("Files/Binaries/SubscribeAck_SingleTopic.packet")]
		[InlineData("Files/Binaries/Unsubscribe_MultiTopic.packet")]
		[InlineData("Files/Binaries/Unsubscribe_SingleTopic.packet")]
		[InlineData("Files/Binaries/UnsubscribeAck.packet")]
		public void when_reading_complete_bytes_then_notifies(string packetPath)
		{
			var socket = new Mock<IBufferedChannel<byte>>();
			var receiver = new Subject<byte>();

			socket.Setup(s => s.Receiver).Returns(receiver);

			var channel = new BinaryChannel (socket.Object);
			var receivedPacket = default(byte[]);

			channel.Receiver.Subscribe(packet => {
				receivedPacket = packet;
			});

			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			
			var readPacket = Packet.ReadAllBytes (packetPath);

			foreach (var @byte in readPacket) {
				receiver.OnNext (@byte);
			}

			Assert.Equal (readPacket, receivedPacket);
		}

		[Theory]
		[InlineData("Files/Binaries/Connect_Full.packet")]
		[InlineData("Files/Binaries/Connect_Min.packet")]
		[InlineData("Files/Binaries/ConnectAck.packet")]
		[InlineData("Files/Binaries/Disconnect.packet")]
		[InlineData("Files/Binaries/PingRequest.packet")]
		[InlineData("Files/Binaries/PingResponse.packet")]
		[InlineData("Files/Binaries/Publish_Full.packet")]
		[InlineData("Files/Binaries/Publish_Min.packet")]
		[InlineData("Files/Binaries/PublishAck.packet")]
		[InlineData("Files/Binaries/PublishComplete.packet")]
		[InlineData("Files/Binaries/PublishReceived.packet")]
		[InlineData("Files/Binaries/PublishRelease.packet")]
		[InlineData("Files/Binaries/Subscribe_MultiTopic.packet")]
		[InlineData("Files/Binaries/Subscribe_SingleTopic.packet")]
		[InlineData("Files/Binaries/SubscribeAck_MultiTopic.packet")]
		[InlineData("Files/Binaries/SubscribeAck_SingleTopic.packet")]
		[InlineData("Files/Binaries/Unsubscribe_MultiTopic.packet")]
		[InlineData("Files/Binaries/Unsubscribe_SingleTopic.packet")]
		[InlineData("Files/Binaries/UnsubscribeAck.packet")]
		public void when_reading_incomplete_bytes_then_does_not_notify(string packetPath)
		{
			var socket = new Mock<IBufferedChannel<byte>>();
			var receiver = new Subject<byte>();

			socket.Setup(s => s.Receiver).Returns(receiver);

			var channel = new BinaryChannel (socket.Object);
			var receivedPacket = default(byte[]);

			channel.Receiver.Subscribe(packet => {
				receivedPacket = packet;
			});

			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			
			var readPacket = Packet.ReadAllBytes (packetPath);

			readPacket = readPacket.Bytes (0, readPacket.Length - 2);

			foreach (var @byte in readPacket) {
				receiver.OnNext (@byte);
			}

			Assert.Null (receivedPacket);
		}

		[Theory]
		[InlineData("Files/Binaries/Connect_Full.packet")]
		[InlineData("Files/Binaries/Connect_Min.packet")]
		[InlineData("Files/Binaries/ConnectAck.packet")]
		[InlineData("Files/Binaries/Disconnect.packet")]
		[InlineData("Files/Binaries/PingRequest.packet")]
		[InlineData("Files/Binaries/PingResponse.packet")]
		[InlineData("Files/Binaries/Publish_Full.packet")]
		[InlineData("Files/Binaries/Publish_Min.packet")]
		[InlineData("Files/Binaries/PublishAck.packet")]
		[InlineData("Files/Binaries/PublishComplete.packet")]
		[InlineData("Files/Binaries/PublishReceived.packet")]
		[InlineData("Files/Binaries/PublishRelease.packet")]
		[InlineData("Files/Binaries/Subscribe_MultiTopic.packet")]
		[InlineData("Files/Binaries/Subscribe_SingleTopic.packet")]
		[InlineData("Files/Binaries/SubscribeAck_MultiTopic.packet")]
		[InlineData("Files/Binaries/SubscribeAck_SingleTopic.packet")]
		[InlineData("Files/Binaries/Unsubscribe_MultiTopic.packet")]
		[InlineData("Files/Binaries/Unsubscribe_SingleTopic.packet")]
		[InlineData("Files/Binaries/UnsubscribeAck.packet")]
		public async Task when_writing_bytes_then_inner_channel_is_notified(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			
			var bytes = Packet.ReadAllBytes (packetPath);

			var receiver = new Subject<byte> ();
			var innerChannel = new Mock<IBufferedChannel<byte>>();

			innerChannel.Setup (x => x.Receiver).Returns (receiver);
			innerChannel.Setup (x => x.SendAsync (It.IsAny<byte[]> ()))
				.Returns (Task.Delay (0));

			var channel = new BinaryChannel (innerChannel.Object);

			await channel.SendAsync (bytes);

			innerChannel.Verify (x => x.SendAsync (It.Is<byte[]> (b => b.ToList ().SequenceEqual (bytes))));
		}
	}
}
