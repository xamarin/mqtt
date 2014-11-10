using System;
using System.Linq;
using System.Reactive;
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
		public void when_socket_connected_then_times_out_if_no_connect_packet ()
		{
			var sockets = new Subject<IBufferedChannel<byte>> ();
			var seconds = new Subject<Unit> ();
			var server = new Server (sockets, seconds, Mock.Of<IPacketChannelFactory> (x => x.CreateChannel (It.IsAny<IBufferedChannel<byte>> ()) ==
				Mock.Of<IChannel<IPacket>>(c => c.Receiver == new Subject<IPacket> ())), Mock.Of<IMessagingHandler>());
			var socket = new Mock<IBufferedChannel<byte>> ();

			sockets.OnNext (socket.Object);

			for (int i = 0; i < 60; i++) {
				seconds.OnNext (Unit.Default);
			}

			socket.Verify (x => x.Close ());
		}

		[Fact]
		public void when_server_disposed_then_pending_connection_disposed ()
		{
			var sockets = new Subject<IBufferedChannel<byte>> ();
			var seconds = new Subject<Unit> ();
			var server = new Server (sockets, seconds, Mock.Of<IPacketChannelFactory> (x => x.CreateChannel (It.IsAny<IBufferedChannel<byte>> ()) ==
				Mock.Of<IChannel<IPacket>>(c => c.Receiver == new Subject<IPacket> ())), Mock.Of<IMessagingHandler>());
			var socket = new Mock<IBufferedChannel<byte>> ();

			sockets.OnNext (socket.Object);

			server.Close ();

			socket.Verify (x => x.Close ());
		}

		[Fact]
		public void when_connect_received_then_does_not_timeout ()
		{
			var sockets = new Subject<IBufferedChannel<byte>> ();
			var seconds = new Subject<Unit> ();

			var packets = new Subject<IPacket> ();
			var channel = Mock.Of<IChannel<IPacket>>(c => c.Receiver == packets);
			var factory = Mock.Of<IPacketChannelFactory> (x => x.CreateChannel (It.IsAny<IBufferedChannel<byte>> ()) == channel);
			var handler = Mock.Of<IMessagingHandler>();

			var server = new Server (sockets, seconds, factory, handler);
			var receiver = new Subject<byte> ();
			var socket = new Mock<IBufferedChannel<byte>> ();

			socket.Setup (x => x.Receiver).Returns (receiver);

			sockets.OnNext (socket.Object);
			packets.OnNext (new Connect ());

			for (int i = 0; i < 60; i++) {
				seconds.OnNext (Unit.Default);
			}

			socket.Verify (x => x.Close (), Times.Never ());
		}

		[Fact]
		public void when_packets_completed_then_disposes_socket ()
		{
			var sockets = new Subject<IBufferedChannel<byte>> ();
			var seconds = new Subject<Unit> ();

			var packets = new Subject<IPacket> ();
			var channel = Mock.Of<IChannel<IPacket>>(c => c.Receiver == packets);
			var factory = Mock.Of<IPacketChannelFactory> (x => x.CreateChannel (It.IsAny<IBufferedChannel<byte>> ()) == channel);
			var handler = Mock.Of<IMessagingHandler>();

			var server = new Server (sockets, seconds, factory, handler);
			var receiver = new Subject<byte> ();
			var socket = new Mock<IBufferedChannel<byte>> ();

			socket.Setup (x => x.Receiver).Returns (receiver);

			sockets.OnNext (socket.Object);
			packets.OnCompleted ();

			socket.Verify (x => x.Close ());
		}

		[Fact]
		public void when_packets_error_then_disposes_socket_and_decreases_connections ()
		{
			var sockets = new Subject<IBufferedChannel<byte>> ();
			var seconds = new Subject<Unit> ();

			IObserver<IPacket> observer = null;
			var packets = new Mock<IObservable<IPacket>> ();

			packets.Setup (x => x.Subscribe (It.IsAny<IObserver<IPacket>> ()))
				.Callback<IObserver<IPacket>> (o => observer = o)
				.Returns (Mock.Of<IDisposable> ());

			var channel = Mock.Of<IChannel<IPacket>>(c => c.Receiver == packets.Object);
			var factory = new Mock<IPacketChannelFactory> ();

			factory.Setup (x => x.CreateChannel (It.IsAny<IBufferedChannel<byte>> ()))
				.Returns (channel);

			var handler = Mock.Of<IMessagingHandler>();

			var server = new Server (sockets, seconds, factory.Object, handler);
			var receiver = new Subject<byte> ();
			var socket = new Mock<IBufferedChannel<byte>> ();

			socket.Setup (x => x.Receiver).Returns (receiver);

			sockets.OnNext (socket.Object);
			observer.OnError (new Exception ("Protocol exception"));

			socket.Verify (x => x.Close ());
			Assert.Equal (0, server.ActiveSockets);
		}

		[Fact]
		public void when_socket_received_then_active_sockets_increases ()
		{
			var sockets = new Subject<IBufferedChannel<byte>> ();
			var seconds = new Subject<Unit> ();
			var packets = new Subject<IPacket> ();
			var channel = Mock.Of<IChannel<IPacket>>(c => c.Receiver == packets);
			var factory = Mock.Of<IPacketChannelFactory> (x => x.CreateChannel (It.IsAny<IBufferedChannel<byte>> ()) == channel);
			var handler = Mock.Of<IMessagingHandler>();

			var server = new Server (sockets, seconds, factory, handler);

			sockets.OnNext (Mock.Of<IBufferedChannel<byte>> (x => x.Receiver == new Subject<byte> ()));

			Assert.Equal (1, server.ActiveSockets);
		}

		[Fact]
		public void when_connect_received_then_client_ids_increases ()
		{
			var sockets = new Subject<IBufferedChannel<byte>> ();
			var seconds = new Subject<Unit> ();
			var packets = new Subject<IPacket> ();
			var channel = Mock.Of<IChannel<IPacket>>(c => c.Receiver == packets);
			var factory = Mock.Of<IPacketChannelFactory> (x => x.CreateChannel (It.IsAny<IBufferedChannel<byte>> ()) == channel);
			var handler = Mock.Of<IMessagingHandler>();

			var server = new Server (sockets, seconds, factory, handler);
			var receiver = new Subject<byte> ();
			var socket = new Mock<IBufferedChannel<byte>> ();

			socket.Setup (x => x.Receiver).Returns (receiver);

			sockets.OnNext (socket.Object);

			var clientId = "FooClient";

			packets.OnNext (new Connect { ClientId = clientId });

			Assert.Equal (1, server.ActiveClients.Count ());
			Assert.Equal (clientId, server.ActiveClients.First ());
		}
	}
}
