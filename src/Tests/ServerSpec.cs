using System;
using System.Reactive.Subjects;
using Hermes;
using Hermes.Packets;
using Moq;
using Xunit;

namespace Tests
{
	public class ServerSpec
	{
		[Fact]
		public void when_server_does_not_start_then_connections_are_ignored ()
		{
			var sockets = new Subject<IChannel<byte[]>> ();
			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 60);

			var packets = new Subject<IPacket> ();
			var packetChannel = Mock.Of<IChannel<IPacket>>(c => c.Receiver == packets);
			var factory = Mock.Of<IPacketChannelFactory> (x => x.Create (It.IsAny<IChannel<byte[]>> ()) == packetChannel);
			var protocolChannel = new Mock<IChannel<IPacket>> ();

			protocolChannel
				.Setup (c => c.IsConnected)
				.Returns (true);
			protocolChannel
				.Setup (c => c.Sender)
				.Returns(new Subject<IPacket> ());
			protocolChannel
				.Setup (c => c.Receiver)
				.Returns(packets);

			var adapter = new Mock<IPacketChannelAdapter> ();
			var connectionProvider = new Mock<IConnectionProvider> ();

			adapter.Setup (a => a.Adapt (It.IsAny<IChannel<IPacket>> ())).Returns (protocolChannel.Object);

			var server = new Server (sockets, factory, adapter.Object, connectionProvider.Object, configuration);

			sockets.OnNext (Mock.Of<IChannel<byte[]>> (x => x.Receiver == new Subject<byte[]> ()));

			Assert.Equal (0, server.ActiveChannels);
		}

		[Fact]
		public void when_connection_established_then_active_connections_increases ()
		{
			var sockets = new Subject<IChannel<byte[]>> ();
			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 60);

			var packets = new Subject<IPacket> ();
			var packetChannel = Mock.Of<IChannel<IPacket>>(c => c.Receiver == packets);
			var factory = Mock.Of<IPacketChannelFactory> (x => x.Create (It.IsAny<IChannel<byte[]>> ()) == packetChannel);
			var protocolChannel = new Mock<IChannel<IPacket>> ();

			protocolChannel
				.Setup (c => c.IsConnected)
				.Returns (true);
			protocolChannel
				.Setup (c => c.Sender)
				.Returns(new Subject<IPacket> ());
			protocolChannel
				.Setup (c => c.Receiver)
				.Returns(packets);

			var adapter = new Mock<IPacketChannelAdapter> ();
			var connectionProvider = new Mock<IConnectionProvider> ();

			adapter.Setup (a => a.Adapt (It.IsAny<IChannel<IPacket>> ())).Returns (protocolChannel.Object);

			var server = new Server (sockets, factory, adapter.Object, connectionProvider.Object, configuration);

			server.Start ();

			sockets.OnNext (Mock.Of<IChannel<byte[]>> (x => x.Receiver == new Subject<byte[]> ()));

			Assert.Equal (1, server.ActiveChannels);
		}

		[Fact]
		public void when_server_closed_then_pending_connection_is_closed ()
		{
			var sockets = new Subject<IChannel<byte[]>> ();
			var protocolChannel = new Mock<IChannel<IPacket>> ();

			protocolChannel
				.Setup (c => c.IsConnected)
				.Returns (true);
			protocolChannel
				.Setup (c => c.Sender)
				.Returns(new Subject<IPacket> ());
			protocolChannel
				.Setup (c => c.Receiver)
				.Returns(new Subject<IPacket> ());

			var adapter = new Mock<IPacketChannelAdapter> ();
			var connectionProvider = new Mock<IConnectionProvider> ();

			adapter.Setup (a => a.Adapt (It.IsAny<IChannel<IPacket>> ())).Returns (protocolChannel.Object);

			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 60);
			var server = new Server (sockets, Mock.Of<IPacketChannelFactory> (x => x.Create (It.IsAny<IChannel<byte[]>> ()) ==
				Mock.Of<IChannel<IPacket>>(c => c.Receiver == new Subject<IPacket> ())), adapter.Object, connectionProvider.Object, configuration);

			server.Start ();

			var socket = new Mock<IChannel<byte[]>> ();

			sockets.OnNext (socket.Object);

			server.Stop ();

			protocolChannel.Verify (x => x.Dispose ());
		}

		[Fact]
		public void when_receiver_error_then_closes_connection_and_decreases_connection_list ()
		{
			var sockets = new Subject<IChannel<byte[]>> ();
			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 60);

			IObserver<IPacket> observer = null;
			var packets = new Mock<IObservable<IPacket>> ();

			packets.Setup (x => x.Subscribe (It.IsAny<IObserver<IPacket>> ()))
				.Callback<IObserver<IPacket>> (o => observer = o)
				.Returns (Mock.Of<IDisposable> ());

			var packetChannel = Mock.Of<IChannel<IPacket>>(c => c.Receiver == packets.Object);
			var factory = new Mock<IPacketChannelFactory> ();

			factory.Setup (x => x.Create (It.IsAny<IChannel<byte[]>> ()))
				.Returns (packetChannel);

			var protocolChannel = new Mock<IChannel<IPacket>> ();

			protocolChannel
				.Setup (c => c.IsConnected)
				.Returns (true);
			protocolChannel
				.Setup (c => c.Sender)
				.Returns(new Subject<IPacket> ());
			protocolChannel
				.Setup (c => c.Receiver)
				.Returns(packets.Object);

			var adapter = new Mock<IPacketChannelAdapter> ();
			var connectionProvider = new Mock<IConnectionProvider> ();

			adapter.Setup (a => a.Adapt (It.IsAny<IChannel<IPacket>> ())).Returns (protocolChannel.Object);

			var server = new Server (sockets, factory.Object, adapter.Object, connectionProvider.Object, configuration);
			var receiver = new Subject<byte[]> ();
			var socket = new Mock<IChannel<byte[]>> ();

			socket.Setup (x => x.Receiver).Returns (receiver);

			server.Start ();

			sockets.OnNext (socket.Object);
			observer.OnError (new Exception ("Protocol exception"));

			protocolChannel.Verify (x => x.Dispose ());
			Assert.Equal (0, server.ActiveChannels);
		}

		[Fact]
		public void when_sender_error_then_closes_connection_and_decreases_connection_list ()
		{
			var sockets = new Subject<IChannel<byte[]>> ();
			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 60);

			var packets = new Subject<IPacket>();
			var packetChannel = Mock.Of<IChannel<IPacket>>(c => c.Receiver == packets);
			var factory = new Mock<IPacketChannelFactory> ();

			factory.Setup (x => x.Create (It.IsAny<IChannel<byte[]>> ()))
				.Returns (packetChannel);

			IObserver<IPacket> observer = null;
			var sender = new Mock<IObservable<IPacket>> ();

			sender.Setup (x => x.Subscribe (It.IsAny<IObserver<IPacket>> ()))
				.Callback<IObserver<IPacket>> (o => observer = o)
				.Returns (Mock.Of<IDisposable> ());

			var protocolChannel = new Mock<IChannel<IPacket>> ();

			protocolChannel
				.Setup (c => c.IsConnected)
				.Returns (true);
			protocolChannel
				.Setup (c => c.Sender)
				.Returns(sender.Object);
			protocolChannel
				.Setup (c => c.Receiver)
				.Returns(packets);

			var adapter = new Mock<IPacketChannelAdapter> ();
			var connectionProvider = new Mock<IConnectionProvider> ();

			adapter.Setup (a => a.Adapt (It.IsAny<IChannel<IPacket>> ())).Returns (protocolChannel.Object);

			var server = new Server (sockets, factory.Object, adapter.Object, connectionProvider.Object, configuration);
			var receiver = new Subject<byte[]> ();
			var socket = new Mock<IChannel<byte[]>> ();

			socket.Setup (x => x.Receiver).Returns (receiver);

			server.Start ();

			sockets.OnNext (socket.Object);
			observer.OnError (new Exception ("Protocol exception"));

			protocolChannel.Verify (x => x.Dispose ());
			Assert.Equal (0, server.ActiveChannels);
		}
	}
}
