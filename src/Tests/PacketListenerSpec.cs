﻿using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Net.Mqtt;
using System.Net.Mqtt.Flows;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Storage;
using Moq;
using Xunit;

namespace Tests
{
	public class PacketListenerSpec
	{
		[Fact]
		public void when_packet_is_received_then_it_is_dispatched_to_proper_flow()
		{
			var connectionProvider = new Mock<IConnectionProvider> ();
			var flowProvider = new Mock<IProtocolFlowProvider> ();
			var repositoryProvider = Mock.Of<IRepositoryProvider> ();

			var flow = new Mock<IProtocolFlow>();

			flowProvider.Setup (p => p.GetFlow (It.IsAny<PacketType> ())).Returns (flow.Object);

			var configuration = new ProtocolConfiguration { WaitingTimeoutSecs = 10 };
			var receiver = new Subject<IPacket> ();
			var packetChannel = new Mock<IChannel<IPacket>> ();

			packetChannel.Setup (c => c.Receiver).Returns (receiver);
			packetChannel.Setup (c => c.Sender).Returns (new Subject<IPacket> ());

			var listener = new ServerPacketListener (packetChannel.Object, connectionProvider.Object, flowProvider.Object, configuration);

			listener.Listen ();

			var clientId = Guid.NewGuid().ToString();
			var connect = new Connect (clientId, cleanSession: true);
			var publish = new Publish (Guid.NewGuid ().ToString (), QualityOfService.AtMostOnce, false, false);

			receiver.OnNext (connect);
			receiver.OnNext (publish);

			flowProvider.Verify (p => p.GetFlow (It.Is<PacketType> (t => t == PacketType.Publish)));
			flow.Verify (f => f.ExecuteAsync (It.Is<string> (s => s == clientId), It.Is<IPacket> (p => p is Connect), It.Is<IChannel<IPacket>>(c => c == packetChannel.Object)));
			flow.Verify (f => f.ExecuteAsync (It.Is<string> (s => s == clientId), It.Is<IPacket> (p => p is Publish), It.Is<IChannel<IPacket>>(c => c == packetChannel.Object)));
		}

		[Fact]
		public void when_connect_received_then_client_id_is_added()
		{
			var connectionProvider = new Mock<IConnectionProvider> ();
			var flowProvider = Mock.Of<IProtocolFlowProvider> ();
			var repositoryProvider = Mock.Of<IRepositoryProvider> ();
			var configuration = new ProtocolConfiguration { WaitingTimeoutSecs = 10 };
			var receiver = new Subject<IPacket> ();
			var packetChannel = new Mock<IChannel<IPacket>> ();

			packetChannel.Setup (c => c.Receiver).Returns (receiver);
			packetChannel.Setup (c => c.Sender).Returns (new Subject<IPacket> ());

			var listener = new ServerPacketListener (packetChannel.Object, connectionProvider.Object, flowProvider, configuration);

			listener.Listen ();

			var clientId = Guid.NewGuid().ToString();
			var connect = new Connect (clientId, cleanSession: true);

			receiver.OnNext (connect);

			connectionProvider.Verify (m => m.AddConnection (It.Is<string> (s => s == clientId), It.Is<IChannel<IPacket>> (c => c == packetChannel.Object)));
		}

		[Fact]
		public void when_no_connect_is_received_then_times_out()
		{
			var connectionProvider = new Mock<IConnectionProvider> ();
			var flowProvider = Mock.Of<IProtocolFlowProvider> ();
			var repositoryProvider = Mock.Of<IRepositoryProvider> ();
			var waitingTimeout = 1;
			var configuration = new ProtocolConfiguration { WaitingTimeoutSecs = waitingTimeout };
			var receiver = new Subject<IPacket> ();
			var packetChannel = new Mock<IChannel<IPacket>> ();

			packetChannel.Setup (c => c.Receiver).Returns (receiver);
			packetChannel.Setup (c => c.Sender).Returns (new Subject<IPacket> ());

			var listener = new ServerPacketListener (packetChannel.Object, connectionProvider.Object, flowProvider, configuration);

			listener.Listen ();

			var timeoutSignal = new ManualResetEventSlim (initialState: false);
			
			listener.Packets.Subscribe (_ => { }, ex => {
				timeoutSignal.Set ();
			});

			var timeoutOccurred = timeoutSignal.Wait ((waitingTimeout + 1) * 1000);

			Assert.True (timeoutOccurred);
		}

		[Fact]
		public void when_connect_is_received_then_does_not_time_out()
		{
			var connectionProvider = new Mock<IConnectionProvider> ();
			var flowProvider = Mock.Of<IProtocolFlowProvider> ();
			var repositoryProvider = Mock.Of<IRepositoryProvider> ();
			var waitingTimeout = 1;
			var configuration = new ProtocolConfiguration { WaitingTimeoutSecs = waitingTimeout };
			var receiver = new Subject<IPacket> ();
			var packetChannel = new Mock<IChannel<IPacket>> ();

			packetChannel.Setup (c => c.Receiver).Returns (receiver);
			packetChannel.Setup (c => c.Sender).Returns (new Subject<IPacket> ());

			var listener = new ServerPacketListener (packetChannel.Object, connectionProvider.Object, flowProvider, configuration);

			listener.Listen ();
			
			var timeoutOccured = false;
			
			listener.Packets.Subscribe (_ => { }, ex => {
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
			var receiver = new Subject<IPacket> ();
			var packetChannel = new Mock<IChannel<IPacket>> ();

			packetChannel.Setup (c => c.Receiver).Returns (receiver);
			packetChannel.Setup (c => c.Sender).Returns (new Subject<IPacket> ());

			var listener = new ServerPacketListener (packetChannel.Object, connectionProvider.Object, flowProvider, configuration);

			listener.Listen ();
			
			var errorOccured = false;
			
			listener.Packets.Subscribe (_ => { }, ex => {
				errorOccured = true;
			});

			receiver.OnNext (new PingRequest());

			Assert.True (errorOccured);
		}

		[Fact]
		public void when_second_connect_received_then_fails()
		{
			var connectionProvider = new Mock<IConnectionProvider> ();

			connectionProvider.Setup (p => p.RemoveConnection (It.IsAny<string> ()));

			var flowProvider = Mock.Of<IProtocolFlowProvider> ();
			var configuration = new ProtocolConfiguration { WaitingTimeoutSecs = 10 };
			var receiver = new Subject<IPacket> ();
			var packetChannel = new Mock<IChannel<IPacket>> ();

			packetChannel.Setup (c => c.Receiver).Returns (receiver);
			packetChannel.Setup (c => c.Sender).Returns (new Subject<IPacket> ());

			var listener = new ServerPacketListener (packetChannel.Object, connectionProvider.Object, flowProvider, configuration);

			listener.Listen ();
			
			var errorSignal = new ManualResetEventSlim();
			
			listener.Packets.Subscribe (_ => { }, ex => {
				errorSignal.Set ();
			});

			var clientId = Guid.NewGuid().ToString();
			var connect = new Connect (clientId, cleanSession: true);

			receiver.OnNext (connect);
			receiver.OnNext (connect);

			var errorOccured = errorSignal.Wait (TimeSpan.FromSeconds(1));

			Assert.True (errorOccured);
			connectionProvider.Verify (p => p.RemoveConnection (It.Is<string> (s => s == clientId)));
		}

		[Fact]
		public void when_keep_alive_enabled_and_no_packet_received_then_times_out ()
		{
			var connectionProvider = new Mock<IConnectionProvider> ();
			var flowProvider = new Mock<IProtocolFlowProvider> ();

			flowProvider.Setup (p => p.GetFlow (It.IsAny<PacketType> ()))
				.Returns (Mock.Of<IProtocolFlow> ());

			var configuration = new ProtocolConfiguration { WaitingTimeoutSecs = 10 };
			var receiver = new Subject<IPacket> ();
			var sender = new Subject<IPacket> ();
			var packetChannelMock = new Mock<IChannel<IPacket>> ();

			packetChannelMock.Setup (c => c.Receiver).Returns (receiver);
			packetChannelMock.Setup (c => c.Sender).Returns (sender);

			var packetChannel = packetChannelMock.Object;

			var listener = new ServerPacketListener (packetChannel, connectionProvider.Object, flowProvider.Object, configuration);

			listener.Listen ();

			var timeoutSignal = new ManualResetEventSlim (initialState: false);
			
			listener.Packets.Subscribe (_ => { }, ex => {
				timeoutSignal.Set ();
			});

			var clientId = Guid.NewGuid().ToString();
			var keepAlive = (ushort)1;
			var connect = new Connect (clientId, cleanSession: true) { KeepAlive = keepAlive };

			receiver.OnNext (connect);

			var connectAck = new ConnectAck (ConnectionStatus.Accepted, existingSession: false);

			sender.OnNext (connectAck);

			var timeoutOccurred = timeoutSignal.Wait(((int)((keepAlive + 1) * 1.5) * 1000));

			Assert.True (timeoutOccurred);
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
			var receiver = new Subject<IPacket> ();
			var packetChannel = new Mock<IChannel<IPacket>> ();

			packetChannel.Setup (c => c.Receiver).Returns (receiver);
			packetChannel.Setup (c => c.Sender).Returns (new Subject<IPacket> ());

			var listener = new ServerPacketListener (packetChannel.Object, connectionProvider.Object, flowProvider.Object, configuration);

			listener.Listen ();
			
			var timeoutOccured = false;
			
			listener.Packets.Subscribe (_ => { }, ex => {
				timeoutOccured = true;
			});

			var clientId = Guid.NewGuid().ToString();
			var keepAlive = (ushort)1;
			var connect = new Connect (clientId, cleanSession: true) { KeepAlive = keepAlive };

			receiver.OnNext (connect);
			packetChannel.Object.SendAsync(new ConnectAck (ConnectionStatus.Accepted, existingSession: false)).Wait();
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
			var receiver = new Subject<IPacket> ();
			var packetChannel = new Mock<IChannel<IPacket>> ();

			packetChannel.Setup (c => c.Receiver).Returns (receiver);
			packetChannel.Setup (c => c.Sender).Returns (new Subject<IPacket> ());

			var listener = new ServerPacketListener (packetChannel.Object, connectionProvider.Object, flowProvider.Object, configuration);

			listener.Listen ();

			var timeoutSignal = new ManualResetEventSlim (initialState: false);
			
			listener.Packets.Subscribe (_ => { }, ex => {
				timeoutSignal.Set ();
			});

			var clientId = Guid.NewGuid().ToString();
			var connect = new Connect (clientId, cleanSession: true) { KeepAlive = 0 };

			receiver.OnNext (connect);
			packetChannel.Object.SendAsync(new ConnectAck (ConnectionStatus.Accepted, existingSession: false)).Wait();

			var timeoutOccurred = timeoutSignal.Wait (2000); 

			Assert.False (timeoutOccurred);
		}
	}
}
