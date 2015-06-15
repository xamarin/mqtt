﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Net.Mqtt;
using System.Net.Mqtt.Flows;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Storage;
using Moq;
using Xunit;
using System.Threading.Tasks;

namespace Tests.Flows
{
	public class PublishSenderFlowSpec
	{
		[Fact]
		public void when_sending_publish_with_qos1_and_publish_ack_is_not_received_then_publish_is_re_transmitted()
		{
			var clientId = Guid.NewGuid ().ToString ();

			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 1 && c.MaximumQualityOfService == QualityOfService.AtLeastOnce);
			var connectionProvider = new Mock<IConnectionProvider> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>> ()))
				.Returns (new ClientSession {
					ClientId = clientId,
					PendingMessages = new List<PendingMessage> { new PendingMessage() }
				});

			var flow = new PublishSenderFlow (sessionRepository.Object, configuration);

			var topic = "foo/bar";
			var packetId = (ushort?)new Random ().Next (0, ushort.MaxValue);
			var publish = new Publish (topic, QualityOfService.AtLeastOnce, retain: false, duplicated: false, packetId: packetId);

			publish.Payload = Encoding.UTF8.GetBytes ("Publish Receiver Flow Test");

			var receiver = new Subject<IPacket> ();
			var sender = new Subject<IPacket> ();
			var channel = new Mock<IChannel<IPacket>> ();

			channel.Setup (c => c.IsConnected).Returns (true);
			channel.Setup (c => c.Receiver).Returns (receiver);
			channel.Setup (c => c.Sender).Returns (sender);
			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sender.OnNext (packet))
				.Returns (Task.Delay (0));

			connectionProvider.Setup (m => m.GetConnection (It.IsAny<string> ())).Returns (channel.Object);

			var retrySignal = new ManualResetEventSlim (initialState: false);
			var retries = 0;

			sender.Subscribe (p => {
				if (p is Publish) {
					retries++;
				}

				if (retries > 1) {
					retrySignal.Set ();
				}
			});

			flow.SendPublishAsync (clientId, publish, channel.Object);

			var retried = retrySignal.Wait (2000);

			Assert.True (retried);
			channel.Verify (c => c.SendAsync (It.Is<IPacket> (p => p is Publish  && 
				((Publish)p).Topic == topic && 
				((Publish)p).QualityOfService == QualityOfService.AtLeastOnce &&
				((Publish)p).PacketId == packetId)), Times.AtLeast(2));
		}

		[Fact]
		public void when_sending_publish_with_qos2_and_publish_received_is_not_received_then_publish_is_re_transmitted()
		{
			var clientId = Guid.NewGuid ().ToString ();

			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 1 && c.MaximumQualityOfService == QualityOfService.ExactlyOnce);
			var connectionProvider = new Mock<IConnectionProvider> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>> ()))
				.Returns (new ClientSession {
					ClientId = clientId,
					PendingMessages = new List<PendingMessage> { new PendingMessage() }
				});

			var flow = new PublishSenderFlow (sessionRepository.Object, configuration);

			var topic = "foo/bar";
			var packetId = (ushort?)new Random ().Next (0, ushort.MaxValue);
			var publish = new Publish (topic, QualityOfService.ExactlyOnce, retain: false, duplicated: false, packetId: packetId);

			publish.Payload = Encoding.UTF8.GetBytes ("Publish Receiver Flow Test");

			var receiver = new Subject<IPacket> ();
			var sender = new Subject<IPacket> ();
			var channel = new Mock<IChannel<IPacket>> ();

			channel.Setup (c => c.IsConnected).Returns (true);
			channel.Setup (c => c.Receiver).Returns (receiver);
			channel.Setup (c => c.Sender).Returns (sender);
			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sender.OnNext (packet))
				.Returns (Task.Delay (0));

			connectionProvider.Setup (m => m.GetConnection (It.IsAny<string> ())).Returns (channel.Object);

			var retrySignal = new ManualResetEventSlim (initialState: false);
			var retries = 0;

			sender.Subscribe (p => {
				if (p is Publish) {
					retries++;
				}

				if (retries > 1) {
					retrySignal.Set ();
				}
			});

			flow.SendPublishAsync (clientId, publish, channel.Object);

			var retried = retrySignal.Wait (2000);

			Assert.True (retried);
			channel.Verify (c => c.SendAsync (It.Is<IPacket> (p => p is Publish  && 
				((Publish)p).Topic == topic && 
				((Publish)p).QualityOfService == QualityOfService.ExactlyOnce &&
				((Publish)p).PacketId == packetId)), Times.AtLeast(2));
		}

		[Fact]
		public void when_sending_publish_received_then_publish_release_is_sent()
		{
			var clientId = Guid.NewGuid ().ToString ();

			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 10);
			var connectionProvider = new Mock<IConnectionProvider> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>> ()))
				.Returns (new ClientSession {
					ClientId = clientId,
					PendingMessages = new List<PendingMessage> { new PendingMessage() }
				});

			var flow = new PublishSenderFlow (sessionRepository.Object, configuration);

			var packetId = (ushort)new Random ().Next (0, ushort.MaxValue);
			var publishReceived = new PublishReceived (packetId);
			var receiver = new Subject<IPacket> ();
			var sender = new Subject<IPacket> ();
			var channel = new Mock<IChannel<IPacket>> ();

			channel.Setup (c => c.IsConnected).Returns (true);
			channel.Setup (c => c.Receiver).Returns (receiver);
			channel.Setup (c => c.Sender).Returns (sender);
			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sender.OnNext (packet))
				.Returns (Task.Delay (0));

			connectionProvider.Setup (m => m.GetConnection (It.Is<string> (s => s == clientId))).Returns (channel.Object);

			var ackSentSignal = new ManualResetEventSlim (initialState: false);

			sender.Subscribe (p => {
				if (p is PublishRelease) {
					ackSentSignal.Set ();
				}
			});

			flow.ExecuteAsync (clientId, publishReceived, channel.Object);

			var ackSent = ackSentSignal.Wait (2000);

			Assert.True (ackSent);
			channel.Verify (c => c.SendAsync (It.Is<IPacket> (p => p is PublishRelease 
				&& (p as PublishRelease).PacketId == packetId)), Times.AtLeastOnce);
		}

		[Fact]
		public void when_sending_publish_received_and_no_complete_is_sent_after_receiving_publish_release_then_publish_release_is_re_transmitted()
		{
			var clientId = Guid.NewGuid ().ToString ();

			var configuration = Mock.Of<ProtocolConfiguration> (c => c.WaitingTimeoutSecs == 1);
			var connectionProvider = new Mock<IConnectionProvider> ();
			var sessionRepository = new Mock<IRepository<ClientSession>> ();

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>> ()))
				.Returns (new ClientSession {
					ClientId = clientId,
					PendingMessages = new List<PendingMessage> { new PendingMessage() }
				});

			var flow = new PublishSenderFlow (sessionRepository.Object, configuration);

			var packetId = (ushort)new Random ().Next (0, ushort.MaxValue);
			var publishReceived = new PublishReceived (packetId);
			var receiver = new Subject<IPacket> ();
			var sender = new Subject<IPacket> ();
			var channel = new Mock<IChannel<IPacket>> ();

			channel.Setup (c => c.IsConnected).Returns (true);
			channel.Setup (c => c.Receiver).Returns (receiver);
			channel.Setup (c => c.Sender).Returns (sender);
			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (packet => sender.OnNext (packet))
				.Returns (Task.Delay (0));

			connectionProvider.Setup (m => m.GetConnection (It.Is<string> (s => s == clientId))).Returns (channel.Object);

			var ackSentSignal = new ManualResetEventSlim (initialState: false);

			sender.Subscribe (p => {
				if (p is PublishRelease) {
					ackSentSignal.Set ();
				}
			});

			flow.ExecuteAsync (clientId, publishReceived, channel.Object);

			var ackSent = ackSentSignal.Wait (2000);

			Assert.True (ackSent);
			channel.Verify (c => c.SendAsync (It.Is<IPacket> (p => p is PublishRelease 
				&& (p as PublishRelease).PacketId == packetId)), Times.AtLeast(1));
		}
	}
}
