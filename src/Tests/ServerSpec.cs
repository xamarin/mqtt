using Moq;
using System;
using System.Net.Mqtt;
using System.Net.Mqtt.Flows;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Server;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class ServerSpec
	{
		[Fact]
		public void when_server_does_not_start_then_connections_are_ignored ()
		{
			var sockets = new Subject<IMqttChannel<byte[]>> ();
			var channelProvider = new Mock<IMqttChannelListener> ();

			channelProvider
				.Setup (p => p.AcceptChannelsAsync ())
				.Returns (sockets);

			var configuration = Mock.Of<MqttConfiguration> (c => c.WaitingTimeoutSecs == 60);

			var packets = new Subject<IPacket> ();
			var packetChannel = new Mock<IMqttChannel<IPacket>>();
			var factory = Mock.Of<IPacketChannelFactory> (x => x.Create (It.IsAny<IMqttChannel<byte[]>> ()) == packetChannel.Object);

			packetChannel
				.Setup (c => c.IsConnected)
				.Returns (true);
			packetChannel
				.Setup (c => c.Sender)
				.Returns(new Subject<IPacket> ());
			packetChannel
				.Setup (c => c.Receiver)
				.Returns(packets);

			var flowProvider = Mock.Of<IProtocolFlowProvider> ();
			var connectionProvider = new Mock<IConnectionProvider> ();

			var server = new Server (channelProvider.Object, factory, flowProvider, connectionProvider.Object, Mock.Of<ISubject<MqttUndeliveredMessage>>(), configuration);

			sockets.OnNext (Mock.Of<IMqttChannel<byte[]>> (x => x.Receiver == new Subject<byte[]> ()));
			sockets.OnNext (Mock.Of<IMqttChannel<byte[]>> (x => x.Receiver == new Subject<byte[]> ()));
			sockets.OnNext (Mock.Of<IMqttChannel<byte[]>> (x => x.Receiver == new Subject<byte[]> ()));

			Assert.Equal (0, server.ActiveChannels);
		}

		[Fact]
		public void when_connection_established_then_active_connections_increases ()
		{
			var sockets = new Subject<IMqttChannel<byte[]>> ();
			var channelProvider = new Mock<IMqttChannelListener> ();

			channelProvider
				.Setup (p => p.AcceptChannelsAsync ())
				.Returns (sockets);

			var configuration = Mock.Of<MqttConfiguration> (c => c.WaitingTimeoutSecs == 60);

			var packets = new Subject<IPacket> ();
			var packetChannel = new Mock<IMqttChannel<IPacket>>();
			var factory = Mock.Of<IPacketChannelFactory> (x => x.Create (It.IsAny<IMqttChannel<byte[]>> ()) == packetChannel.Object);

			packetChannel
				.Setup (c => c.IsConnected)
				.Returns (true);
			packetChannel
				.Setup (c => c.Sender)
				.Returns(new Subject<IPacket> ());
			packetChannel
				.Setup (c => c.Receiver)
				.Returns(packets);

			var flowProvider = Mock.Of<IProtocolFlowProvider> ();
			var connectionProvider = new Mock<IConnectionProvider> ();

			var server = new Server (channelProvider.Object, factory, flowProvider, connectionProvider.Object, Mock.Of<ISubject<MqttUndeliveredMessage>> (), configuration);

			server.Start ();

			sockets.OnNext (Mock.Of<IMqttChannel<byte[]>> (x => x.Receiver == new Subject<byte[]> ()));
			sockets.OnNext (Mock.Of<IMqttChannel<byte[]>> (x => x.Receiver == new Subject<byte[]> ()));
			sockets.OnNext (Mock.Of<IMqttChannel<byte[]>> (x => x.Receiver == new Subject<byte[]> ()));

			Assert.Equal (3, server.ActiveChannels);
		}

		[Fact]
		public void when_server_closed_then_pending_connection_is_closed ()
		{
			var sockets = new Subject<IMqttChannel<byte[]>> ();
			var channelProvider = new Mock<IMqttChannelListener> ();

			channelProvider
				.Setup (p => p.AcceptChannelsAsync ())
				.Returns (sockets);

			var packetChannel = new Mock<IMqttChannel<IPacket>> ();

			packetChannel
				.Setup (c => c.IsConnected)
				.Returns (true);
			packetChannel
				.Setup (c => c.Sender)
				.Returns(new Subject<IPacket> ());
			packetChannel
				.Setup (c => c.Receiver)
				.Returns(new Subject<IPacket> ());

			var flowProvider = Mock.Of<IProtocolFlowProvider> ();
			var connectionProvider = new Mock<IConnectionProvider> ();

			var configuration = Mock.Of<MqttConfiguration> (c => c.WaitingTimeoutSecs == 60);

			var server = new Server (channelProvider.Object, Mock.Of<IPacketChannelFactory> (x => x.Create (It.IsAny<IMqttChannel<byte[]>> ()) == packetChannel.Object), 
				flowProvider, connectionProvider.Object, Mock.Of<ISubject<MqttUndeliveredMessage>> (), configuration);

			server.Start ();

			var socket = new Mock<IMqttChannel<byte[]>> ();

			sockets.OnNext (socket.Object);

			server.Stop ();

			packetChannel.Verify (x => x.Dispose ());
		}

		[Fact]
		public async Task when_receiver_error_then_closes_connection ()
		{
			var sockets = new Subject<IMqttChannel<byte[]>> ();
			var channelProvider = new Mock<IMqttChannelListener> ();

			channelProvider
				.Setup (p => p.AcceptChannelsAsync ())
				.Returns (sockets);

			var configuration = Mock.Of<MqttConfiguration> (c => c.WaitingTimeoutSecs == 60);

			var packets = new Subject<IPacket> ();

			var packetChannel = new Mock<IMqttChannel<IPacket>> ();
			var factory = new Mock<IPacketChannelFactory> ();

			packetChannel
				.Setup (c => c.IsConnected)
				.Returns (true);
			packetChannel
				.Setup (c => c.Sender)
				.Returns(new Subject<IPacket> ());
			packetChannel
				.Setup (c => c.Receiver)
				.Returns(packets);

			factory.Setup (x => x.Create (It.IsAny<IMqttChannel<byte[]>> ()))
				.Returns (packetChannel.Object);

			var flowProvider = Mock.Of<IProtocolFlowProvider> ();
			var connectionProvider = new Mock<IConnectionProvider> ();

			var server = new Server (channelProvider.Object, factory.Object, flowProvider, connectionProvider.Object, Mock.Of<ISubject<MqttUndeliveredMessage>> (), configuration);
			var receiver = new Subject<byte[]> ();
			var socket = new Mock<IMqttChannel<byte[]>> ();

			socket.Setup (x => x.Receiver).Returns (receiver);

			server.Start ();

			sockets.OnNext (socket.Object);

			try {
				packets.OnError (new Exception ("Protocol exception"));
			} catch (Exception) {
			}

            await Task.Delay (TimeSpan.FromMilliseconds (1000));

            packetChannel.Verify (x => x.Dispose ());
		}
	}
}
