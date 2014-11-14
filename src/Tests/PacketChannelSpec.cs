using System;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Hermes;
using Hermes.Packets;
using Moq;
using Xunit;
using Xunit.Extensions;

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

			var topicEvaluator = Mock.Of<ITopicEvaluator> ();
			var factory = new PacketChannelFactory (topicEvaluator);
			var channel = factory.CreateChannel (bufferedChannel.Object);

			Assert.NotNull (channel);
		}

		[Theory]
		[InlineData("Files/Binaries/Connect_Full.packet", "Files/Packets/Connect_Full.json", typeof(Connect))]
		[InlineData("Files/Binaries/Connect_Min.packet", "Files/Packets/Connect_Min.json", typeof(Connect))]
		[InlineData("Files/Binaries/ConnectAck.packet", "Files/Packets/ConnectAck.json", typeof(ConnectAck))]
		[InlineData("Files/Binaries/Publish_Full.packet", "Files/Packets/Publish_Full.json", typeof(Publish))]
		[InlineData("Files/Binaries/Publish_Min.packet", "Files/Packets/Publish_Min.json", typeof(Publish))]
		[InlineData("Files/Binaries/PublishAck.packet", "Files/Packets/PublishAck.json", typeof(PublishAck))]
		[InlineData("Files/Binaries/PublishComplete.packet", "Files/Packets/PublishComplete.json", typeof(PublishComplete))]
		[InlineData("Files/Binaries/PublishReceived.packet", "Files/Packets/PublishReceived.json", typeof(PublishReceived))]
		[InlineData("Files/Binaries/PublishRelease.packet", "Files/Packets/PublishRelease.json", typeof(PublishRelease))]
		[InlineData("Files/Binaries/Subscribe_MultiTopic.packet", "Files/Packets/Subscribe_MultiTopic.json", typeof(Subscribe))]
		[InlineData("Files/Binaries/Subscribe_SingleTopic.packet", "Files/Packets/Subscribe_SingleTopic.json", typeof(Subscribe))]
		[InlineData("Files/Binaries/SubscribeAck_MultiTopic.packet", "Files/Packets/SubscribeAck_MultiTopic.json", typeof(SubscribeAck))]
		[InlineData("Files/Binaries/SubscribeAck_SingleTopic.packet", "Files/Packets/SubscribeAck_SingleTopic.json", typeof(SubscribeAck))]
		[InlineData("Files/Binaries/Unsubscribe_MultiTopic.packet", "Files/Packets/Unsubscribe_MultiTopic.json", typeof(Unsubscribe))]
		[InlineData("Files/Binaries/Unsubscribe_SingleTopic.packet", "Files/Packets/Unsubscribe_SingleTopic.json", typeof(Unsubscribe))]
		[InlineData("Files/Binaries/UnsubscribeAck.packet", "Files/Packets/UnsubscribeAck.json", typeof(UnsubscribeAck))]
		public void when_reading_bytes_from_source_then_notifies_packet(string packetPath, string jsonPath, Type packetType)
		{
			var receiver = new Subject<byte[]> ();
			var innerChannel = new Mock<IChannel<byte[]>>();

			innerChannel.Setup (x => x.Receiver).Returns (receiver);

			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedPacket = Packet.ReadPacket (jsonPath, packetType) as IPacket;

			var manager = new Mock<IPacketManager> ();

			manager.Setup(x => x.GetAsync(It.IsAny<byte[]>()))
				.Returns(Task.FromResult<IPacket>(expectedPacket));

			var channel = new PacketChannel (innerChannel.Object, manager.Object);

			var receivedPacket = default (IPacket);

			channel.Receiver.Subscribe (packet => {
				receivedPacket = packet;
			});

			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			
			var readPacket = Packet.ReadAllBytes (packetPath);

			receiver.OnNext (readPacket);

			Assert.NotNull (receivedPacket);
			Assert.Equal (expectedPacket, receivedPacket);
		}

		[Theory]
		[InlineData("Files/Binaries/Disconnect.packet", typeof(Disconnect))]
		[InlineData("Files/Binaries/PingRequest.packet", typeof(PingRequest))]
		[InlineData("Files/Binaries/PingResponse.packet", typeof(PingResponse))]
		public void when_reading_bytes_then_notifies_packet(string packetPath, Type packetType)
		{
			var receiver = new Subject<byte[]> ();
			var innerChannel = new Mock<IChannel<byte[]>>();

			innerChannel.Setup (x => x.Receiver).Returns (receiver);

			var expectedPacket = Activator.CreateInstance (packetType);
			var manager = new Mock<IPacketManager> ();

			manager.Setup(x => x.GetAsync(It.IsAny<byte[]>()))
				.Returns(Task.FromResult<IPacket>((IPacket)expectedPacket));

			var channel = new PacketChannel (innerChannel.Object, manager.Object);

			var receivedPacket = default (IPacket);

			channel.Receiver.Subscribe (packet => {
				receivedPacket = packet;
			});

			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			
			var readPacket = Packet.ReadAllBytes (packetPath);

			receiver.OnNext (readPacket);

			Assert.NotNull (receivedPacket);
			Assert.Equal (packetType, receivedPacket.GetType ());
		}

		[Theory]
		[InlineData("Files/Binaries/Connect_Full.packet", "Files/Packets/Connect_Full.json", typeof(Connect))]
		[InlineData("Files/Binaries/Connect_Min.packet", "Files/Packets/Connect_Min.json", typeof(Connect))]
		[InlineData("Files/Binaries/ConnectAck.packet", "Files/Packets/ConnectAck.json", typeof(ConnectAck))]
		[InlineData("Files/Binaries/Publish_Full.packet", "Files/Packets/Publish_Full.json", typeof(Publish))]
		[InlineData("Files/Binaries/Publish_Min.packet", "Files/Packets/Publish_Min.json", typeof(Publish))]
		[InlineData("Files/Binaries/PublishAck.packet", "Files/Packets/PublishAck.json", typeof(PublishAck))]
		[InlineData("Files/Binaries/PublishComplete.packet", "Files/Packets/PublishComplete.json", typeof(PublishComplete))]
		[InlineData("Files/Binaries/PublishReceived.packet", "Files/Packets/PublishReceived.json", typeof(PublishReceived))]
		[InlineData("Files/Binaries/PublishRelease.packet", "Files/Packets/PublishRelease.json", typeof(PublishRelease))]
		[InlineData("Files/Binaries/Subscribe_MultiTopic.packet", "Files/Packets/Subscribe_MultiTopic.json", typeof(Subscribe))]
		[InlineData("Files/Binaries/Subscribe_SingleTopic.packet", "Files/Packets/Subscribe_SingleTopic.json", typeof(Subscribe))]
		[InlineData("Files/Binaries/SubscribeAck_MultiTopic.packet", "Files/Packets/SubscribeAck_MultiTopic.json", typeof(SubscribeAck))]
		[InlineData("Files/Binaries/SubscribeAck_SingleTopic.packet", "Files/Packets/SubscribeAck_SingleTopic.json", typeof(SubscribeAck))]
		[InlineData("Files/Binaries/Unsubscribe_MultiTopic.packet", "Files/Packets/Unsubscribe_MultiTopic.json", typeof(Unsubscribe))]
		[InlineData("Files/Binaries/Unsubscribe_SingleTopic.packet", "Files/Packets/Unsubscribe_SingleTopic.json", typeof(Unsubscribe))]
		[InlineData("Files/Binaries/UnsubscribeAck.packet", "Files/Packets/UnsubscribeAck.json", typeof(UnsubscribeAck))]
		public async Task when_writing_packet_from_source_then_inner_channel_is_notified(string packetPath, string jsonPath, Type packetType)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			
			var bytes = Packet.ReadAllBytes (packetPath);

			var receiver = new Subject<byte[]> ();
			var innerChannel = new Mock<IChannel<byte[]>>();

			innerChannel.Setup (x => x.Receiver).Returns (receiver);
			innerChannel.Setup (x => x.SendAsync (It.IsAny<byte[]> ()))
				.Returns (Task.Delay (0));

			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);
			
			var packet = Packet.ReadPacket(jsonPath, packetType) as IPacket;

			var manager = new Mock<IPacketManager> ();

			manager.Setup(x => x.GetAsync(It.IsAny<IPacket>()))
				.Returns(Task.FromResult(bytes));

			var channel = new PacketChannel (innerChannel.Object, manager.Object);

			await channel.SendAsync (packet);

			innerChannel.Verify (x => x.SendAsync (It.Is<byte[]> (b => b.ToList ().SequenceEqual (bytes))));
			manager.Verify (x => x.GetAsync (It.Is<IPacket> (p => Convert.ChangeType(p, packetType) == packet)));
		}

		[Theory]
		[InlineData("Files/Binaries/Disconnect.packet", typeof(Disconnect))]
		[InlineData("Files/Binaries/PingRequest.packet", typeof(PingRequest))]
		[InlineData("Files/Binaries/PingResponse.packet", typeof(PingResponse))]
		public async Task when_writing_packet_then_inner_channel_is_notified(string packetPath, Type packetType)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			
			var bytes = Packet.ReadAllBytes (packetPath);

			var receiver = new Subject<byte[]> ();
			var innerChannel = new Mock<IChannel<byte[]>>();

			innerChannel.Setup (x => x.Receiver).Returns (receiver);
			innerChannel.Setup (x => x.SendAsync (It.IsAny<byte[]> ()))
				.Returns (Task.Delay (0));

			var packet = Activator.CreateInstance (packetType) as IPacket;

			var manager = new Mock<IPacketManager> ();

			manager.Setup(x => x.GetAsync(It.IsAny<IPacket>()))
				.Returns(Task.FromResult(bytes));

			var channel = new PacketChannel (innerChannel.Object, manager.Object);

			await channel.SendAsync (packet);

			innerChannel.Verify (x => x.SendAsync (It.Is<byte[]> (b => b.ToList ().SequenceEqual (bytes))));
			manager.Verify (x => x.GetAsync (It.Is<IPacket> (p => Convert.ChangeType(p, packetType) == packet)));
		}

		[Fact]
		public void when_packet_channel_error_then_notifies()
		{
			var receiver = new Subject<byte[]> ();
			var innerChannel = new Mock<IChannel<byte[]>>();

			innerChannel.Setup (x => x.Receiver).Returns (receiver);

			var manager = new Mock<IPacketManager> ();

			var channel = new PacketChannel (innerChannel.Object, manager.Object);

			var errorMessage = "Packet Exception";

			receiver.OnError (new ProtocolException(errorMessage));

			var errorReceived = default (Exception);

			channel.Receiver.Subscribe (_ => { }, ex => {
				errorReceived = ex;
			});

			Assert.NotNull (errorReceived);
			Assert.True (errorReceived is ProtocolException);
			Assert.Equal (errorMessage, (errorReceived as ProtocolException).Message);
		}
	}
}
