using System;
using System.Linq;
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
		public void when_connection_established_then_active_connections_increases ()
		{
			var sockets = new Subject<IBufferedChannel<byte>> ();
			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 60);

			var packets = new Subject<IPacket> ();
			var packetChannel = Mock.Of<IChannel<IPacket>>(c => c.Receiver == packets);
			var factory = Mock.Of<IPacketChannelFactory> (x => x.CreateChannel (It.IsAny<IBufferedChannel<byte>> ()) == packetChannel);
			var protocolChannel = Mock.Of<IChannel<IPacket>> (c => c.Sender == new Subject<IPacket> () && c.Receiver == packets);
			var adapter = new Mock<IPacketChannelAdapter> ();

			adapter.Setup (a => a.Adapt (It.IsAny<IChannel<IPacket>> ())).Returns (protocolChannel);

			var server = new Server (sockets, factory, adapter.Object, configuration);

			sockets.OnNext (Mock.Of<IBufferedChannel<byte>> (x => x.Receiver == new Subject<byte> ()));

			Assert.Equal (1, server.ActiveSockets);
		}

		[Fact]
		public void when_connect_is_received_then_client_list_does_not_increase()
		{
			var sockets = new Subject<IBufferedChannel<byte>> ();
			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 60);

			var packets = new Subject<IPacket> ();
			var packetChannel = Mock.Of<IChannel<IPacket>>(c => c.Receiver == packets);
			var factory = Mock.Of<IPacketChannelFactory> (x => x.CreateChannel (It.IsAny<IBufferedChannel<byte>> ()) == packetChannel);
			var sender = new Subject<IPacket> ();
			var protocolChannel = Mock.Of<IChannel<IPacket>> (c => c.Sender == sender && c.Receiver == packets);
			var adapter = new Mock<IPacketChannelAdapter> ();

			adapter.Setup (a => a.Adapt (It.IsAny<IChannel<IPacket>> ())).Returns (protocolChannel);

			var server = new Server (sockets, factory, adapter.Object, configuration);

			sockets.OnNext (Mock.Of<IBufferedChannel<byte>> (x => x.Receiver == new Subject<byte> ()));
			packets.OnNext (new Connect (Guid.NewGuid ().ToString (), cleanSession: true));

			Assert.Equal (0, server.ActiveClients.Count());
		}

		[Fact]
		public void when_connect_ack_is_returned_then_client_list_increases()
		{
			var sockets = new Subject<IBufferedChannel<byte>> ();
			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 60);

			var packets = new Subject<IPacket> ();
			var packetChannel = Mock.Of<IChannel<IPacket>>(c => c.Receiver == packets);
			var factory = Mock.Of<IPacketChannelFactory> (x => x.CreateChannel (It.IsAny<IBufferedChannel<byte>> ()) == packetChannel);
			var sender = new Subject<IPacket> ();
			var protocolChannel = Mock.Of<IChannel<IPacket>> (c => c.Sender == sender && c.Receiver == packets);
			var adapter = new Mock<IPacketChannelAdapter> ();

			adapter.Setup (a => a.Adapt (It.IsAny<IChannel<IPacket>> ())).Returns (protocolChannel);

			var server = new Server (sockets, factory, adapter.Object, configuration);

			sockets.OnNext (Mock.Of<IBufferedChannel<byte>> (x => x.Receiver == new Subject<byte> ()));
			sender.OnNext (new ConnectAck (ConnectionStatus.Accepted, existingSession: false));

			Assert.Equal (1, server.ActiveClients.Count());
		}

		[Fact]
		public void when_server_closed_then_pending_connection_is_closed ()
		{
			var sockets = new Subject<IBufferedChannel<byte>> ();
			var protocolChannel = Mock.Of<IChannel<IPacket>> (c => c.Sender == new Subject<IPacket> () && c.Receiver == new Subject<IPacket>());
			var adapter = new Mock<IPacketChannelAdapter> ();

			adapter.Setup (a => a.Adapt (It.IsAny<IChannel<IPacket>> ())).Returns (protocolChannel);

			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 60);
			var server = new Server (sockets, Mock.Of<IPacketChannelFactory> (x => x.CreateChannel (It.IsAny<IBufferedChannel<byte>> ()) ==
				Mock.Of<IChannel<IPacket>>(c => c.Receiver == new Subject<IPacket> ())), adapter.Object, configuration);
			
			var socket = new Mock<IBufferedChannel<byte>> ();

			sockets.OnNext (socket.Object);
			server.Close ();

			socket.Verify (x => x.Close ());
		}

		[Fact]
		public void when_receiver_completed_then_closes_connection ()
		{
			var sockets = new Subject<IBufferedChannel<byte>> ();
			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 60);

			var packets = new Subject<IPacket> ();
			var packetChannel = Mock.Of<IChannel<IPacket>>(c => c.Receiver == packets);
			var factory = Mock.Of<IPacketChannelFactory> (x => x.CreateChannel (It.IsAny<IBufferedChannel<byte>> ()) == packetChannel);
			var protocolChannel = Mock.Of<IChannel<IPacket>> (c => c.Sender == new Subject<IPacket>() && c.Receiver == packets);
			var adapter = new Mock<IPacketChannelAdapter> ();

			adapter.Setup (a => a.Adapt (It.IsAny<IChannel<IPacket>> ())).Returns (protocolChannel);

			var server = new Server (sockets, factory, adapter.Object, configuration);
			var receiver = new Subject<byte> ();
			var socket = new Mock<IBufferedChannel<byte>> ();

			socket.Setup (x => x.Receiver).Returns (receiver);

			sockets.OnNext (socket.Object);
			packets.OnCompleted ();

			socket.Verify (x => x.Close ());
		}

		[Fact]
		public void when_receiver_error_then_closes_connection_and_decreases_connection_list ()
		{
			var sockets = new Subject<IBufferedChannel<byte>> ();
			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 60);

			IObserver<IPacket> observer = null;
			var packets = new Mock<IObservable<IPacket>> ();

			packets.Setup (x => x.Subscribe (It.IsAny<IObserver<IPacket>> ()))
				.Callback<IObserver<IPacket>> (o => observer = o)
				.Returns (Mock.Of<IDisposable> ());

			var packetChannel = Mock.Of<IChannel<IPacket>>(c => c.Receiver == packets.Object);
			var factory = new Mock<IPacketChannelFactory> ();

			factory.Setup (x => x.CreateChannel (It.IsAny<IBufferedChannel<byte>> ()))
				.Returns (packetChannel);

			var protocolChannel = Mock.Of<IChannel<IPacket>> (c => c.Sender == new Subject<IPacket>() && c.Receiver == packets.Object);
			var adapter = new Mock<IPacketChannelAdapter> ();

			adapter.Setup (a => a.Adapt (It.IsAny<IChannel<IPacket>> ())).Returns (protocolChannel);

			var server = new Server (sockets, factory.Object, adapter.Object, configuration);
			var receiver = new Subject<byte> ();
			var socket = new Mock<IBufferedChannel<byte>> ();

			socket.Setup (x => x.Receiver).Returns (receiver);

			sockets.OnNext (socket.Object);
			observer.OnError (new Exception ("Protocol exception"));

			socket.Verify (x => x.Close ());
			Assert.Equal (0, server.ActiveSockets);
		}

		[Fact]
		public void when_sender_completed_then_closes_connection ()
		{
			var sockets = new Subject<IBufferedChannel<byte>> ();
			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 60);

			var packets = new Subject<IPacket> ();
			var packetChannel = Mock.Of<IChannel<IPacket>>(c => c.Receiver == packets);
			var factory = Mock.Of<IPacketChannelFactory> (x => x.CreateChannel (It.IsAny<IBufferedChannel<byte>> ()) == packetChannel);
			var sender = new Subject<IPacket> ();
			var protocolChannel = Mock.Of<IChannel<IPacket>> (c => c.Sender == sender && c.Receiver == packets);
			var adapter = new Mock<IPacketChannelAdapter> ();

			adapter.Setup (a => a.Adapt (It.IsAny<IChannel<IPacket>> ())).Returns (protocolChannel);

			var server = new Server (sockets, factory, adapter.Object, configuration);
			var receiver = new Subject<byte> ();
			var socket = new Mock<IBufferedChannel<byte>> ();

			socket.Setup (x => x.Receiver).Returns (receiver);

			sockets.OnNext (socket.Object);
			sender.OnCompleted ();

			socket.Verify (x => x.Close ());
		}

		[Fact]
		public void when_sender_error_then_closes_connection_and_decreases_connection_list ()
		{
			var sockets = new Subject<IBufferedChannel<byte>> ();
			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 60);

			var packets = new Subject<IPacket>();
			var packetChannel = Mock.Of<IChannel<IPacket>>(c => c.Receiver == packets);
			var factory = new Mock<IPacketChannelFactory> ();

			factory.Setup (x => x.CreateChannel (It.IsAny<IBufferedChannel<byte>> ()))
				.Returns (packetChannel);

			IObserver<IPacket> observer = null;
			var sender = new Mock<IObservable<IPacket>> ();

			sender.Setup (x => x.Subscribe (It.IsAny<IObserver<IPacket>> ()))
				.Callback<IObserver<IPacket>> (o => observer = o)
				.Returns (Mock.Of<IDisposable> ());

			var protocolChannel = Mock.Of<IChannel<IPacket>> (c => c.Sender == sender.Object && c.Receiver == packets);
			var adapter = new Mock<IPacketChannelAdapter> ();

			adapter.Setup (a => a.Adapt (It.IsAny<IChannel<IPacket>> ())).Returns (protocolChannel);

			var server = new Server (sockets, factory.Object, adapter.Object, configuration);
			var receiver = new Subject<byte> ();
			var socket = new Mock<IBufferedChannel<byte>> ();

			socket.Setup (x => x.Receiver).Returns (receiver);

			sockets.OnNext (socket.Object);
			observer.OnError (new Exception ("Protocol exception"));

			socket.Verify (x => x.Close ());
			Assert.Equal (0, server.ActiveSockets);
		}
	}
}
