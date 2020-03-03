using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Mqtt;
using System.Net.Mqtt.Sdk.Formatters;
using System.Net.Mqtt.Sdk.Packets;
using Moq;
using Xunit;
using System.Net.Mqtt.Sdk;

namespace Tests.Formatters
{
	internal class EmptyPacketFormatterSpec
	{
		readonly Mock<IMqttChannel<IPacket>> packetChannel;
		readonly Mock<IMqttChannel<byte[]>> byteChannel;

		public EmptyPacketFormatterSpec ()
		{
			packetChannel = new Mock<IMqttChannel<IPacket>> ();
			byteChannel = new Mock<IMqttChannel<byte[]>> ();
		}
		
		[Theory]
		[InlineData("Files/Binaries/PingResponse.packet", MqttPacketType.PingResponse, typeof(PingResponse))]
		[InlineData("Files/Binaries/PingRequest.packet", MqttPacketType.PingRequest, typeof(PingRequest))]
		[InlineData("Files/Binaries/Disconnect.packet", MqttPacketType.Disconnect, typeof(Disconnect))]
		public async Task when_reading_empty_packet_then_succeeds(string packetPath, MqttPacketType packetType, Type type)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = GetFormatter (packetType, type);
			var packet = Packet.ReadAllBytes (packetPath);

			var result = await formatter.FormatAsync (packet)
				.ConfigureAwait(continueOnCapturedContext: false);

			Assert.NotNull (result);
		}

		[Theory]
		[InlineData("Files/Binaries/PingResponse_Invalid_HeaderFlag.packet", MqttPacketType.PingResponse, typeof(PingResponse))]
		[InlineData("Files/Binaries/PingRequest_Invalid_HeaderFlag.packet", MqttPacketType.PingRequest, typeof(PingRequest))]
		[InlineData("Files/Binaries/Disconnect_Invalid_HeaderFlag.packet", MqttPacketType.Disconnect, typeof(Disconnect))]
		public void when_reading_invalid_empty_packet_then_fails(string packetPath, MqttPacketType packetType, Type type)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = GetFormatter (packetType, type);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.FormatAsync (packet).Wait());

			Assert.True (ex.InnerException is MqttException);
		}

		[Theory]
		[InlineData("Files/Binaries/PingResponse.packet", MqttPacketType.PingResponse, typeof(PingResponse))]
		[InlineData("Files/Binaries/PingRequest.packet", MqttPacketType.PingRequest, typeof(PingRequest))]
		[InlineData("Files/Binaries/Disconnect.packet", MqttPacketType.Disconnect, typeof(Disconnect))]
		public async Task when_writing_empty_packet_then_succeeds(string packetPath, MqttPacketType packetType, Type type)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var expectedPacket = Packet.ReadAllBytes (packetPath);
			var formatter = GetFormatter (packetType, type);
			var packet = Activator.CreateInstance (type) as IPacket;

			var result = await formatter.FormatAsync (packet)
				.ConfigureAwait(continueOnCapturedContext: false);

			Assert.Equal (expectedPacket, result);
		}

		IFormatter GetFormatter(MqttPacketType packetType, Type type)
		{
			var genericType = typeof (EmptyPacketFormatter<>);
			var formatterType = genericType.MakeGenericType (type);
			var formatter = Activator.CreateInstance (formatterType, packetType) as IFormatter;

			return formatter;
		}
	}
}
