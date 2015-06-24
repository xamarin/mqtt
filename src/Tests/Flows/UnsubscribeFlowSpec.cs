using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Net.Mqtt;
using System.Net.Mqtt.Flows;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Storage;
using Moq;
using Xunit;
using System.Net.Mqtt.Server;

namespace Tests.Flows
{
	public class UnsubscribeFlowSpec
	{
		[Fact]
		public async Task when_unsubscribing_existing_subscriptions_then_subscriptions_are_deleted_and_ack_is_sent()
		{
			var sessionRepository = new Mock<IRepository<ClientSession>> ();
			var clientId = Guid.NewGuid ().ToString ();
			var packetId = (ushort)new Random ().Next (0, ushort.MaxValue);
			var topic = "foo/bar/test";
			var qos = QualityOfService.AtLeastOnce;
			var session = new ClientSession { 
				ClientId = clientId,
				Clean = false, 
				Subscriptions = new List<ClientSubscription> { 
					new ClientSubscription { ClientId = clientId, MaximumQualityOfService = qos, TopicFilter = topic } 
				} 
			};
			var updatedSession = default(ClientSession);

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>> ())).Returns (session);
			sessionRepository.Setup (r => r.Update (It.IsAny<ClientSession> ())).Callback<ClientSession> (s => updatedSession = s);
			
			var unsubscribe = new Unsubscribe (packetId, topic);

			var channel = new Mock<IChannel<IPacket>> ();

			var response = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (p => response = p)
				.Returns(Task.Delay(0));

			var connectionProvider = new Mock<IConnectionProvider> ();

			connectionProvider
				.Setup (p => p.GetConnection (It.Is<string> (c => c == clientId)))
				.Returns (channel.Object);

			var flow = new ServerUnsubscribeFlow (sessionRepository.Object);

			await flow.ExecuteAsync(clientId, unsubscribe, channel.Object)
				.ConfigureAwait(continueOnCapturedContext: false);

			Assert.NotNull (response);
			Assert.Equal (0, updatedSession.Subscriptions.Count);

			var unsubscribeAck = response as UnsubscribeAck;

			Assert.NotNull (unsubscribeAck);
			Assert.Equal (packetId, unsubscribeAck.PacketId);
		}

		[Fact]
		public async Task when_unsubscribing_not_existing_subscriptions_then_ack_is_sent()
		{
			var sessionRepository = new Mock<IRepository<ClientSession>> ();
			var clientId = Guid.NewGuid ().ToString ();
			var packetId = (ushort)new Random ().Next (0, ushort.MaxValue);
			var session = new ClientSession { 
				ClientId = clientId,
				Clean = false
			};

			sessionRepository.Setup (r => r.Get (It.IsAny<Expression<Func<ClientSession, bool>>> ())).Returns (session);

			var unsubscribe = new Unsubscribe (packetId, "foo/bar");

			var channel = new Mock<IChannel<IPacket>> ();

			var response = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (p => response = p)
				.Returns(Task.Delay(0));

			var connectionProvider = new Mock<IConnectionProvider> ();

			connectionProvider
				.Setup (p => p.GetConnection (It.Is<string> (c => c == clientId)))
				.Returns (channel.Object);

			var flow = new ServerUnsubscribeFlow (sessionRepository.Object);

			await flow.ExecuteAsync(clientId, unsubscribe, channel.Object)
				.ConfigureAwait(continueOnCapturedContext: false);

			sessionRepository.Verify (r => r.Delete (It.IsAny<Expression<Func<ClientSession, bool>>> ()), Times.Never);
			Assert.NotNull (response);

			var unsubscribeAck = response as UnsubscribeAck;

			Assert.NotNull (unsubscribeAck);
			Assert.Equal (packetId, unsubscribeAck.PacketId);
		}
	}
}
