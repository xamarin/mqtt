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
			var channel = Mock.Of<IChannel<IPacket>>(c => c.Receiver == packets);
			var factory = Mock.Of<IPacketChannelFactory> (x => x.CreateChannel (It.IsAny<IBufferedChannel<byte>> ()) == channel);
			var context = Mock.Of<ICommunicationContext> (c => c.PendingDeliveries == new Subject<IPacket> ());
			var handler = new Mock<ICommunicationHandler> ();

			handler.Setup (h => h.Handle (It.IsAny<IChannel<IPacket>> ())).Returns (context);

			var server = new Server (sockets, factory, handler.Object, configuration);

			sockets.OnNext (Mock.Of<IBufferedChannel<byte>> (x => x.Receiver == new Subject<byte> ()));

			Assert.Equal (1, server.ActiveSockets);
		}

		[Fact]
		public void when_delivery_is_received_then_packet_is_sent()
		{
			var sockets = new Subject<IBufferedChannel<byte>> ();
			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 60);

			var packets = new Subject<IPacket> ();
			var channel = new Mock<IChannel<IPacket>>();
			var factory = Mock.Of<IPacketChannelFactory> (x => x.CreateChannel (It.IsAny<IBufferedChannel<byte>> ()) == channel.Object);
			var deliveries = new Subject<IPacket> ();
			var context = Mock.Of<ICommunicationContext> (c => c.PendingDeliveries == deliveries);
			var handler = new Mock<ICommunicationHandler> ();

			channel.Setup(c => c.Receiver).Returns(packets);
			handler.Setup (h => h.Handle (It.IsAny<IChannel<IPacket>> ())).Returns (context);

			var server = new Server (sockets, factory, handler.Object, configuration);

			sockets.OnNext (Mock.Of<IBufferedChannel<byte>> (x => x.Receiver == new Subject<byte> ()));
			deliveries.OnNext (new PingResponse());

			channel.Verify (c => c.SendAsync (It.Is<IPacket> (p => p is PingResponse)));
		}

		[Fact]
		public void when_connect_is_received_then_client_list_does_not_increase()
		{
			var sockets = new Subject<IBufferedChannel<byte>> ();
			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 60);

			var packets = new Subject<IPacket> ();
			var channel = Mock.Of<IChannel<IPacket>>(c => c.Receiver == packets);
			var factory = Mock.Of<IPacketChannelFactory> (x => x.CreateChannel (It.IsAny<IBufferedChannel<byte>> ()) == channel);
			var deliveries = new Subject<IPacket> ();
			var context = Mock.Of<ICommunicationContext> (c => c.PendingDeliveries == deliveries);
			var handler = new Mock<ICommunicationHandler> ();

			handler.Setup (h => h.Handle (It.IsAny<IChannel<IPacket>> ())).Returns (context);

			var server = new Server (sockets, factory, handler.Object, configuration);

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
			var channel = Mock.Of<IChannel<IPacket>>(c => c.Receiver == packets);
			var factory = Mock.Of<IPacketChannelFactory> (x => x.CreateChannel (It.IsAny<IBufferedChannel<byte>> ()) == channel);
			var deliveries = new Subject<IPacket> ();
			var context = Mock.Of<ICommunicationContext> (c => c.PendingDeliveries == deliveries);
			var handler = new Mock<ICommunicationHandler> ();

			handler.Setup (h => h.Handle (It.IsAny<IChannel<IPacket>> ())).Returns (context);

			var server = new Server (sockets, factory, handler.Object, configuration);

			sockets.OnNext (Mock.Of<IBufferedChannel<byte>> (x => x.Receiver == new Subject<byte> ()));
			deliveries.OnNext (new ConnectAck (ConnectionStatus.Accepted, existingSession: false));

			Assert.Equal (1, server.ActiveClients.Count());
		}

		[Fact]
		public void when_deliveries_error_then_closes_connection_and_decreases_connection_list ()
		{
			var sockets = new Subject<IBufferedChannel<byte>> ();
			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 60);
			var packets = new Subject<IPacket> ();
			var channel = Mock.Of<IChannel<IPacket>>(c => c.Receiver == packets);
			var factory = new Mock<IPacketChannelFactory> ();

			factory.Setup (x => x.CreateChannel (It.IsAny<IBufferedChannel<byte>> ()))
				.Returns (channel);

			IObserver<IPacket> observer = null;
			var deliveries = new Mock<IObservable<IPacket>> ();

			deliveries.Setup (x => x.Subscribe (It.IsAny<IObserver<IPacket>> ()))
				.Callback<IObserver<IPacket>> (o => observer = o)
				.Returns (Mock.Of<IDisposable> ());

			var context = Mock.Of<ICommunicationContext> (c => c.PendingDeliveries == deliveries.Object);
			var handler = new Mock<ICommunicationHandler> ();

			handler.Setup (h => h.Handle (It.IsAny<IChannel<IPacket>> ())).Returns (context);

			var server = new Server (sockets, factory.Object, handler.Object, configuration);
			var receiver = new Subject<byte> ();
			var socket = new Mock<IBufferedChannel<byte>> ();

			socket.Setup (x => x.Receiver).Returns (receiver);

			sockets.OnNext (socket.Object);
			observer.OnError (new Exception ("Protocol exception"));

			socket.Verify (x => x.Close ());
			Assert.Equal (0, server.ActiveSockets);
		}

		[Fact]
		public void when_deliveries_completed_then_closes_connection_and_decreases_connection_list ()
		{
			var sockets = new Subject<IBufferedChannel<byte>> ();
			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 60);

			var packets = new Subject<IPacket> ();
			var channel = Mock.Of<IChannel<IPacket>>(c => c.Receiver == packets);
			var factory = Mock.Of<IPacketChannelFactory> (x => x.CreateChannel (It.IsAny<IBufferedChannel<byte>> ()) == channel);
			var deliveries = new Subject<IPacket> ();
			var context = Mock.Of<ICommunicationContext> (c => c.PendingDeliveries == deliveries);
			var handler = new Mock<ICommunicationHandler> ();

			handler.Setup (h => h.Handle (It.IsAny<IChannel<IPacket>> ())).Returns (context);

			var server = new Server (sockets, factory, handler.Object, configuration);
			var receiver = new Subject<byte> ();
			var socket = new Mock<IBufferedChannel<byte>> ();

			socket.Setup (x => x.Receiver).Returns (receiver);

			sockets.OnNext (socket.Object);
			deliveries.OnCompleted ();

			socket.Verify (x => x.Close ());
		}

		[Fact]
		public void when_server_closed_then_pending_connection_is_closed ()
		{
			var sockets = new Subject<IBufferedChannel<byte>> ();
			var context = Mock.Of<ICommunicationContext> (c => c.PendingDeliveries == new Subject<IPacket> ());
			var handler = new Mock<ICommunicationHandler> ();

			handler.Setup (h => h.Handle (It.IsAny<IChannel<IPacket>> ())).Returns (context);

			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 60);
			var server = new Server (sockets, Mock.Of<IPacketChannelFactory> (x => x.CreateChannel (It.IsAny<IBufferedChannel<byte>> ()) ==
				Mock.Of<IChannel<IPacket>>(c => c.Receiver == new Subject<IPacket> ())), handler.Object, configuration);
			
			var socket = new Mock<IBufferedChannel<byte>> ();

			sockets.OnNext (socket.Object);
			server.Close ();

			socket.Verify (x => x.Close ());
		}

		[Fact]
		public void when_packets_completed_then_closes_connection ()
		{
			var sockets = new Subject<IBufferedChannel<byte>> ();
			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 60);

			var packets = new Subject<IPacket> ();
			var channel = Mock.Of<IChannel<IPacket>>(c => c.Receiver == packets);
			var factory = Mock.Of<IPacketChannelFactory> (x => x.CreateChannel (It.IsAny<IBufferedChannel<byte>> ()) == channel);
			var context = Mock.Of<ICommunicationContext> (c => c.PendingDeliveries == new Subject<IPacket> ());
			var handler = new Mock<ICommunicationHandler> ();

			handler.Setup (h => h.Handle (It.IsAny<IChannel<IPacket>> ())).Returns (context);

			var server = new Server (sockets, factory, handler.Object, configuration);
			var receiver = new Subject<byte> ();
			var socket = new Mock<IBufferedChannel<byte>> ();

			socket.Setup (x => x.Receiver).Returns (receiver);

			sockets.OnNext (socket.Object);
			packets.OnCompleted ();

			socket.Verify (x => x.Close ());
		}

		[Fact]
		public void when_packets_error_then_closes_connection_and_decreases_connection_list ()
		{
			var sockets = new Subject<IBufferedChannel<byte>> ();
			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 60);

			IObserver<IPacket> observer = null;
			var packets = new Mock<IObservable<IPacket>> ();

			packets.Setup (x => x.Subscribe (It.IsAny<IObserver<IPacket>> ()))
				.Callback<IObserver<IPacket>> (o => observer = o)
				.Returns (Mock.Of<IDisposable> ());

			var channel = Mock.Of<IChannel<IPacket>>(c => c.Receiver == packets.Object);
			var factory = new Mock<IPacketChannelFactory> ();

			factory.Setup (x => x.CreateChannel (It.IsAny<IBufferedChannel<byte>> ()))
				.Returns (channel);

			var context = Mock.Of<ICommunicationContext> (c => c.PendingDeliveries == new Subject<IPacket> ());
			var handler = new Mock<ICommunicationHandler> ();

			handler.Setup (h => h.Handle (It.IsAny<IChannel<IPacket>> ())).Returns (context);

			var server = new Server (sockets, factory.Object, handler.Object, configuration);
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
