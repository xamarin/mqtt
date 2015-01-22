using System;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using System.Threading;
using Hermes;
using Hermes.Flows;
using Hermes.Packets;
using Hermes.Storage;
using Moq;
using Xunit;

namespace Tests
{
	public class PacketChannelAdapterSpec
	{
		[Fact]
		public void when_adapting_packet_channel_then_protocol_channel_is_returned()
		{
			var connectionProvider = new Mock<IConnectionProvider> ();
			var flowProvider = Mock.Of<IProtocolFlowProvider> ();
			var repositoryProvider = Mock.Of<IRepositoryProvider> ();
			var configuration = new ProtocolConfiguration  { WaitingTimeoutSecs = 10 };
			var adapter = new ServerPacketChannelAdapter (connectionProvider.Object, flowProvider, configuration);

			var receiver = new Subject<IPacket> ();
			var packetChannel = new Mock<IChannel<IPacket>> ();

			packetChannel.Setup (c => c.Receiver).Returns (receiver);

			var protocolChannel = adapter.Adapt (packetChannel.Object);

			Assert.NotNull (protocolChannel);
		}

		[Fact]
		public void when_packet_is_received_then_it_is_dispatched_to_proper_flow()
		{
			var connectionProvider = new Mock<IConnectionProvider> ();
			var flowProvider = new Mock<IProtocolFlowProvider> ();
			var repositoryProvider = Mock.Of<IRepositoryProvider> ();

			var flow = new Mock<IProtocolFlow>();

			flowProvider.Setup (p => p.GetFlow (It.IsAny<PacketType> ())).Returns (flow.Object);

			var configuration = new ProtocolConfiguration { WaitingTimeoutSecs = 10 };
			var adapter = new ServerPacketChannelAdapter (connectionProvider.Object, flowProvider.Object, configuration);

			var receiver = new Subject<IPacket> ();
			var packetChannel = new Mock<IChannel<IPacket>> ();

			packetChannel.Setup (c => c.Receiver).Returns (receiver);

			var protocolChannel = adapter.Adapt (packetChannel.Object);

			var clientId = Guid.NewGuid().ToString();
			var connect = new Connect (clientId, cleanSession: true);
			var publish = new Publish (Guid.NewGuid ().ToString (), QualityOfService.AtMostOnce, false, false);

			receiver.OnNext (connect);
			receiver.OnNext (publish);

			flowProvider.Verify (p => p.GetFlow (It.Is<PacketType> (t => t == PacketType.Publish)));
			flow.Verify (f => f.ExecuteAsync (It.Is<string> (s => s == clientId), It.Is<IPacket> (p => p is Connect), It.Is<IChannel<IPacket>>(c => c == protocolChannel)));
			flow.Verify (f => f.ExecuteAsync (It.Is<string> (s => s == clientId), It.Is<IPacket> (p => p is Publish), It.Is<IChannel<IPacket>>(c => c == protocolChannel)));
		}

		[Fact]
		public void when_connect_received_then_client_id_is_added()
		{
			var connectionProvider = new Mock<IConnectionProvider> ();
			var flowProvider = Mock.Of<IProtocolFlowProvider> ();
			var repositoryProvider = Mock.Of<IRepositoryProvider> ();
			var configuration = new ProtocolConfiguration { WaitingTimeoutSecs = 10 };
			var adapter = new ServerPacketChannelAdapter (connectionProvider.Object, flowProvider, configuration);

			var receiver = new Subject<IPacket> ();
			var packetChannel = new Mock<IChannel<IPacket>> ();

			packetChannel.Setup (c => c.Receiver).Returns (receiver);

			var protocolChannel = adapter.Adapt (packetChannel.Object);

			var clientId = Guid.NewGuid().ToString();
			var connect = new Connect (clientId, cleanSession: true);

			receiver.OnNext (connect);

			connectionProvider.Verify (m => m.AddConnection (It.Is<string> (s => s == clientId), It.Is<IChannel<IPacket>> (c => c == protocolChannel)));
		}

		[Fact]
		public void when_no_connect_is_received_then_times_out()
		{
			var connectionProvider = new Mock<IConnectionProvider> ();
			var flowProvider = Mock.Of<IProtocolFlowProvider> ();
			var repositoryProvider = Mock.Of<IRepositoryProvider> ();
			var waitingTimeout = 1;
			var configuration = new ProtocolConfiguration { WaitingTimeoutSecs = waitingTimeout };
			var adapter = new ServerPacketChannelAdapter (connectionProvider.Object, flowProvider, configuration);

			var receiver = new Subject<IPacket> ();
			var packetChannel = new Mock<IChannel<IPacket>> ();

			packetChannel.Setup (c => c.Receiver).Returns (receiver);

			var protocolChannel = adapter.Adapt (packetChannel.Object);
			var timeoutOccured = false;
			
			protocolChannel.Sender.Subscribe (_ => { }, ex => {
				timeoutOccured = true;
			});

			Thread.Sleep ((waitingTimeout + 1) * 1000);

			Assert.True (timeoutOccured);
		}

		[Fact]
		public void when_connect_is_received_then_does_not_time_out()
		{
			var connectionProvider = new Mock<IConnectionProvider> ();
			var flowProvider = Mock.Of<IProtocolFlowProvider> ();
			var repositoryProvider = Mock.Of<IRepositoryProvider> ();
			var waitingTimeout = 1;
			var configuration = new ProtocolConfiguration { WaitingTimeoutSecs = waitingTimeout };
			var adapter = new ServerPacketChannelAdapter (connectionProvider.Object, flowProvider, configuration);

			var receiver = new Subject<IPacket> ();
			var packetChannel = new Mock<IChannel<IPacket>> ();

			packetChannel.Setup (c => c.Receiver).Returns (receiver);

			var protocolChannel = adapter.Adapt (packetChannel.Object);
			var timeoutOccured = false;
			
			protocolChannel.Receiver.Subscribe (_ => { }, ex => {
				timeoutOccured = true;
			});

			var clientId = Guid.NewGuid().ToString();
			var connect = new Connect (clientId, cleanSession: true);

			receiver.OnNext (connect);

			Assert.False (timeoutOccured);
		}

		[Fact]
		public void when_first_packet_is_not_connect_then_fails()
		{
			var connectionProvider = new Mock<IConnectionProvider> ();
			var flowProvider = Mock.Of<IProtocolFlowProvider> ();
			var repositoryProvider = Mock.Of<IRepositoryProvider> ();
			var configuration = new ProtocolConfiguration { WaitingTimeoutSecs = 10 };
			var adapter = new ServerPacketChannelAdapter (connectionProvider.Object, flowProvider, configuration);

			var receiver = new Subject<IPacket> ();
			var packetChannel = new Mock<IChannel<IPacket>> ();

			packetChannel.Setup (c => c.Receiver).Returns (receiver);

			var protocolChannel = adapter.Adapt (packetChannel.Object);
			var errorOccured = false;
			
			protocolChannel.Sender.Subscribe (_ => { }, ex => {
				errorOccured = true;
			});

			receiver.OnNext (new PingRequest());

			Assert.True (errorOccured);
		}

		[Fact]
		public void when_second_connect_received_then_fails()
		{
			var connectionProvider = new Mock<IConnectionProvider> ();
			var flowProvider = Mock.Of<IProtocolFlowProvider> ();
			var configuration = new ProtocolConfiguration { WaitingTimeoutSecs = 10 };
			var adapter = new ServerPacketChannelAdapter (connectionProvider.Object, flowProvider, configuration);

			var receiver = new Subject<IPacket> ();
			var packetChannel = new Mock<IChannel<IPacket>> ();

			packetChannel.Setup (c => c.Receiver).Returns (receiver);

			var protocolChannel = adapter.Adapt (packetChannel.Object);
			var errorOccured = false;
			
			protocolChannel.Sender.Subscribe (_ => { }, ex => {
				errorOccured = true;
			});

			var clientId = Guid.NewGuid().ToString();
			var connect = new Connect (clientId, cleanSession: true);

			receiver.OnNext (connect);
			receiver.OnNext (connect);

			Assert.True (errorOccured);
		}

		[Fact]
		public void when_keep_alive_enabled_and_no_packet_received_then_times_out ()
		{
			var connectionProvider = new Mock<IConnectionProvider> ();
			var flowProvider = new Mock<IProtocolFlowProvider> ();

			flowProvider.Setup (p => p.GetFlow (It.IsAny<PacketType> ()))
				.Returns (Mock.Of<IProtocolFlow> ());

			var configuration = new ProtocolConfiguration { WaitingTimeoutSecs = 10 };
			var adapter = new ServerPacketChannelAdapter (connectionProvider.Object, flowProvider.Object, configuration);

			var receiver = new Subject<IPacket> ();
			var packetChannel = new Mock<IChannel<IPacket>> ();

			packetChannel.Setup (c => c.Receiver).Returns (receiver);

			var protocolChannel = adapter.Adapt (packetChannel.Object);
			var timeoutOccured = false;
			
			protocolChannel.Sender.Subscribe (_ => { }, ex => {
				timeoutOccured = true;
			});

			var clientId = Guid.NewGuid().ToString();
			var keepAlive = (ushort)1;
			var connect = new Connect (clientId, cleanSession: true) { KeepAlive = keepAlive };

			receiver.OnNext (connect);
			protocolChannel.SendAsync(new ConnectAck (ConnectionStatus.Accepted, existingSession: false)).Wait();

			Thread.Sleep ((int)((keepAlive + 1) * 1.5) * 1000);

			Assert.True (timeoutOccured);
		}

		[Fact]
		public void when_keep_alive_enabled_and_packet_received_then_does_not_time_out ()
		{
			var connectionProvider = new Mock<IConnectionProvider> ();
			var flowProvider = new Mock<IProtocolFlowProvider> ();

			flowProvider.Setup (p => p.GetFlow (It.IsAny<PacketType> ()))
				.Returns (Mock.Of<IProtocolFlow> ());

			var repositoryProvider = Mock.Of<IRepositoryProvider> ();
			var configuration = new ProtocolConfiguration { WaitingTimeoutSecs = 10 };
			var adapter = new ServerPacketChannelAdapter (connectionProvider.Object, flowProvider.Object, configuration);

			var receiver = new Subject<IPacket> ();
			var packetChannel = new Mock<IChannel<IPacket>> ();

			packetChannel.Setup (c => c.Receiver).Returns (receiver);

			var protocolChannel = adapter.Adapt (packetChannel.Object);
			var timeoutOccured = false;
			
			protocolChannel.Sender.Subscribe (_ => { }, ex => {
				timeoutOccured = true;
			});

			var clientId = Guid.NewGuid().ToString();
			var keepAlive = (ushort)1;
			var connect = new Connect (clientId, cleanSession: true) { KeepAlive = keepAlive };

			receiver.OnNext (connect);
			protocolChannel.SendAsync(new ConnectAck (ConnectionStatus.Accepted, existingSession: false)).Wait();
			receiver.OnNext (new PingRequest ());

			Assert.False (timeoutOccured);
		}

		[Fact]
		public void when_keep_alive_disabled_and_no_packet_received_then_does_not_time_out ()
		{
			var connectionProvider = new Mock<IConnectionProvider> ();
			var flowProvider = new Mock<IProtocolFlowProvider> ();

			flowProvider.Setup (p => p.GetFlow (It.IsAny<PacketType> ()))
				.Returns (Mock.Of<IProtocolFlow> ());

			var repositoryProvider = Mock.Of<IRepositoryProvider> ();
			var configuration = new ProtocolConfiguration { WaitingTimeoutSecs = 10 };
			var adapter = new ServerPacketChannelAdapter (connectionProvider.Object, flowProvider.Object, configuration);

			var receiver = new Subject<IPacket> ();
			var packetChannel = new Mock<IChannel<IPacket>> ();

			packetChannel.Setup (c => c.Receiver).Returns (receiver);

			var protocolChannel = adapter.Adapt (packetChannel.Object);
			var timeoutOccured = false;
			
			protocolChannel.Sender.Subscribe (_ => { }, ex => {
				timeoutOccured = true;
			});

			var clientId = Guid.NewGuid().ToString();
			var connect = new Connect (clientId, cleanSession: true) { KeepAlive = 0 };

			receiver.OnNext (connect);
			protocolChannel.SendAsync(new ConnectAck (ConnectionStatus.Accepted, existingSession: false)).Wait();

			Thread.Sleep (2000);

			Assert.False (timeoutOccured);
		}
	}
}
