using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Mqtt;
using System.Net.Mqtt.Sdk.Formatters;
using System.Net.Mqtt.Sdk.Packets;
using Moq;
using Xunit;
using Xunit.Extensions;
using System.Net.Mqtt.Sdk;

namespace Tests
{
	internal class PacketManagerSpec
	{
		[Theory]
		[InlineData("Files/Binaries/Connect_Full.packet", "Files/Packets/Connect_Full.json", typeof(Connect), MqttPacketType.Connect)]
		[InlineData("Files/Binaries/Connect_Min.packet", "Files/Packets/Connect_Min.json", typeof(Connect), MqttPacketType.Connect)]
		[InlineData("Files/Binaries/ConnectAck.packet", "Files/Packets/ConnectAck.json", typeof(ConnectAck), MqttPacketType.ConnectAck)]
		[InlineData("Files/Binaries/Publish_Full.packet", "Files/Packets/Publish_Full.json", typeof(Publish), MqttPacketType.Publish)]
		[InlineData("Files/Binaries/Publish_Min.packet", "Files/Packets/Publish_Min.json", typeof(Publish), MqttPacketType.Publish)]
		[InlineData("Files/Binaries/PublishAck.packet", "Files/Packets/PublishAck.json", typeof(PublishAck), MqttPacketType.PublishAck)]
		[InlineData("Files/Binaries/PublishComplete.packet", "Files/Packets/PublishComplete.json", typeof(PublishComplete), MqttPacketType.PublishComplete)]
		[InlineData("Files/Binaries/PublishReceived.packet", "Files/Packets/PublishReceived.json", typeof(PublishReceived), MqttPacketType.PublishReceived)]
		[InlineData("Files/Binaries/PublishRelease.packet", "Files/Packets/PublishRelease.json", typeof(PublishRelease), MqttPacketType.PublishRelease)]
		[InlineData("Files/Binaries/Subscribe_MultiTopic.packet", "Files/Packets/Subscribe_MultiTopic.json", typeof(Subscribe), MqttPacketType.Subscribe)]
		[InlineData("Files/Binaries/Subscribe_SingleTopic.packet", "Files/Packets/Subscribe_SingleTopic.json", typeof(Subscribe), MqttPacketType.Subscribe)]
		[InlineData("Files/Binaries/SubscribeAck_MultiTopic.packet", "Files/Packets/SubscribeAck_MultiTopic.json", typeof(SubscribeAck), MqttPacketType.SubscribeAck)]
		[InlineData("Files/Binaries/SubscribeAck_SingleTopic.packet", "Files/Packets/SubscribeAck_SingleTopic.json", typeof(SubscribeAck), MqttPacketType.SubscribeAck)]
		[InlineData("Files/Binaries/Unsubscribe_MultiTopic.packet", "Files/Packets/Unsubscribe_MultiTopic.json", typeof(Unsubscribe), MqttPacketType.Unsubscribe)]
		[InlineData("Files/Binaries/Unsubscribe_SingleTopic.packet", "Files/Packets/Unsubscribe_SingleTopic.json", typeof(Unsubscribe), MqttPacketType.Unsubscribe)]
		[InlineData("Files/Binaries/UnsubscribeAck.packet", "Files/Packets/UnsubscribeAck.json", typeof(UnsubscribeAck), MqttPacketType.UnsubscribeAck)]
		public async Task when_managing_packet_bytes_then_succeeds(string packetPath, string jsonPath, Type packetType, MqttPacketType type)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			
			var bytes = Packet.ReadAllBytes (packetPath);
			
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);
			
			var packet = Packet.ReadPacket(jsonPath, packetType);
			var formatter = new Mock<IFormatter> ();

			formatter.Setup(f => f.PacketType).Returns(type);

			formatter
				.Setup (f => f.FormatAsync (It.Is<byte[]> (b => b.ToList().SequenceEqual(bytes))))
				.Returns (Task.FromResult<IPacket>((IPacket)packet));

			var packetManager = new PacketManager (formatter.Object);
			var result =  await packetManager.GetPacketAsync (bytes)
				.ConfigureAwait(continueOnCapturedContext: false);

			Assert.Equal (packet, result);
		}

		[Theory]
		[InlineData("Files/Binaries/Connect_Full.packet", "Files/Packets/Connect_Full.json", typeof(Connect), MqttPacketType.Connect)]
		[InlineData("Files/Binaries/Connect_Min.packet", "Files/Packets/Connect_Min.json", typeof(Connect), MqttPacketType.Connect)]
		[InlineData("Files/Binaries/ConnectAck.packet", "Files/Packets/ConnectAck.json", typeof(ConnectAck), MqttPacketType.ConnectAck)]
		[InlineData("Files/Binaries/Publish_Full.packet", "Files/Packets/Publish_Full.json", typeof(Publish), MqttPacketType.Publish)]
		[InlineData("Files/Binaries/Publish_Min.packet", "Files/Packets/Publish_Min.json", typeof(Publish), MqttPacketType.Publish)]
		[InlineData("Files/Binaries/PublishAck.packet", "Files/Packets/PublishAck.json", typeof(PublishAck), MqttPacketType.PublishAck)]
		[InlineData("Files/Binaries/PublishComplete.packet", "Files/Packets/PublishComplete.json", typeof(PublishComplete), MqttPacketType.PublishComplete)]
		[InlineData("Files/Binaries/PublishReceived.packet", "Files/Packets/PublishReceived.json", typeof(PublishReceived), MqttPacketType.PublishReceived)]
		[InlineData("Files/Binaries/PublishRelease.packet", "Files/Packets/PublishRelease.json", typeof(PublishRelease), MqttPacketType.PublishRelease)]
		[InlineData("Files/Binaries/Subscribe_MultiTopic.packet", "Files/Packets/Subscribe_MultiTopic.json", typeof(Subscribe), MqttPacketType.Subscribe)]
		[InlineData("Files/Binaries/Subscribe_SingleTopic.packet", "Files/Packets/Subscribe_SingleTopic.json", typeof(Subscribe), MqttPacketType.Subscribe)]
		[InlineData("Files/Binaries/SubscribeAck_MultiTopic.packet", "Files/Packets/SubscribeAck_MultiTopic.json", typeof(SubscribeAck), MqttPacketType.SubscribeAck)]
		[InlineData("Files/Binaries/SubscribeAck_SingleTopic.packet", "Files/Packets/SubscribeAck_SingleTopic.json", typeof(SubscribeAck), MqttPacketType.SubscribeAck)]
		[InlineData("Files/Binaries/Unsubscribe_MultiTopic.packet", "Files/Packets/Unsubscribe_MultiTopic.json", typeof(Unsubscribe), MqttPacketType.Unsubscribe)]
		[InlineData("Files/Binaries/Unsubscribe_SingleTopic.packet", "Files/Packets/Unsubscribe_SingleTopic.json", typeof(Unsubscribe), MqttPacketType.Unsubscribe)]
		[InlineData("Files/Binaries/UnsubscribeAck.packet", "Files/Packets/UnsubscribeAck.json", typeof(UnsubscribeAck), MqttPacketType.UnsubscribeAck)]
		public async Task when_managing_packet_from_source_then_succeeds(string packetPath, string jsonPath, Type packetType, MqttPacketType type)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);
			
			var packet = Packet.ReadPacket (jsonPath, packetType) as IPacket;
			
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			
			var bytes = Packet.ReadAllBytes (packetPath);
			var formatter = new Mock<IFormatter> ();

			formatter.Setup(f => f.PacketType).Returns(type);

			formatter
				.Setup (f => f.FormatAsync (It.Is<IPacket> (p => Convert.ChangeType(p, packetType) == packet)))
				.Returns (Task.FromResult(bytes));

			var packetManager = new PacketManager (formatter.Object);
			var result = await packetManager.GetBytesAsync (packet)
				.ConfigureAwait(continueOnCapturedContext: false);

			Assert.Equal (bytes, result);
		}

		[Theory]
		[InlineData("Files/Binaries/Disconnect.packet", typeof(Disconnect), MqttPacketType.Disconnect)]
		[InlineData("Files/Binaries/PingRequest.packet", typeof(PingRequest), MqttPacketType.PingRequest)]
		[InlineData("Files/Binaries/PingResponse.packet", typeof(PingResponse), MqttPacketType.PingResponse)]
		public async Task when_managing_packet_then_succeeds(string packetPath, Type packetType, MqttPacketType type)
		{
			var packet = Activator.CreateInstance (packetType) as IPacket;
			
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			
			var bytes = Packet.ReadAllBytes (packetPath);
			var formatter = new Mock<IFormatter> ();

			formatter.Setup(f => f.PacketType).Returns(type);

			formatter
				.Setup (f => f.FormatAsync (It.Is<IPacket> (p => Convert.ChangeType(p, packetType) == packet)))
				.Returns (Task.FromResult(bytes));

			var packetManager = new PacketManager (formatter.Object);
			var result = await packetManager.GetBytesAsync (packet)
				.ConfigureAwait(continueOnCapturedContext: false);

			Assert.Equal (bytes, result);
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
		public void when_managing_unknown_packet_bytes_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var packet = Packet.ReadAllBytes (packetPath);
			var formatter = new Mock<IFormatter> ();
			var packetManager = new PacketManager (formatter.Object);

			var ex = Assert.Throws<AggregateException> (() => packetManager.GetPacketAsync (packet).Wait());

			Assert.True (ex.InnerException is MqttException);
		}

		[Theory]
		[InlineData("Files/Packets/Connect_Full.json")]
		[InlineData("Files/Packets/Connect_Min.json")]
		[InlineData("Files/Packets/ConnectAck.json")]
		[InlineData("Files/Packets/Publish_Full.json")]
		[InlineData("Files/Packets/Publish_Min.json")]
		[InlineData("Files/Packets/PublishAck.json")]
		[InlineData("Files/Packets/PublishComplete.json")]
		[InlineData("Files/Packets/PublishReceived.json")]
		[InlineData("Files/Packets/PublishRelease.json")]
		[InlineData("Files/Packets/Subscribe_MultiTopic.json")]
		[InlineData("Files/Packets/Subscribe_SingleTopic.json")]
		[InlineData("Files/Packets/SubscribeAck_MultiTopic.json")]
		[InlineData("Files/Packets/SubscribeAck_SingleTopic.json")]
		[InlineData("Files/Packets/Unsubscribe_MultiTopic.json")]
		[InlineData("Files/Packets/Unsubscribe_SingleTopic.json")]
		[InlineData("Files/Packets/UnsubscribeAck.json")]
		public void when_managing_unknown_packet_then_fails(string jsonPath)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);
			
			var packet = Packet.ReadPacket<Connect> (jsonPath);
			var formatter = new Mock<IFormatter> ();
			var packetManager = new PacketManager (formatter.Object);

			var ex = Assert.Throws<AggregateException> (() => packetManager.GetBytesAsync (packet).Wait());

			Assert.True (ex.InnerException is MqttException);
		}
	}
}
