using Moq;
using System;
using System.Collections.Generic;
using System.Net.Mqtt;
using System.Net.Mqtt.Sdk;
using System.Net.Mqtt.Sdk.Flows;
using System.Net.Mqtt.Sdk.Packets;
using System.Net.Mqtt.Sdk.Storage;
using System.Threading.Tasks;
using Xunit;

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
			var qos = MqttQualityOfService.AtLeastOnce;
			var session = new ClientSession(clientId, clean: false) {
				Subscriptions = new List<ClientSubscription> { 
						new ClientSubscription { ClientId = clientId, MaximumQualityOfService = qos, TopicFilter = topic } 
					} 
			};
			var updatedSession = default(ClientSession);

			sessionRepository.Setup (r => r.Read (It.IsAny<string> ())).Returns (session);
			sessionRepository.Setup (r => r.Update (It.IsAny<ClientSession> ())).Callback<ClientSession> (s => updatedSession = s);
			
			var unsubscribe = new Unsubscribe (packetId, topic);

			var channel = new Mock<IMqttChannel<IPacket>> ();

			var response = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (p => response = p)
				.Returns(Task.Delay(0));

			var connectionProvider = new Mock<IConnectionProvider> ();

			connectionProvider
				.Setup (p => p.GetConnectionAsync (It.Is<string> (c => c == clientId)))
				.Returns (Task.FromResult(channel.Object));

			var flow = new ServerUnsubscribeFlow (sessionRepository.Object);

			await flow.ExecuteAsync(clientId, unsubscribe, channel.Object)
				.ConfigureAwait(continueOnCapturedContext: false);

			Assert.NotNull (response);
			Assert.Empty (updatedSession.Subscriptions);

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
			var session = new ClientSession(clientId, clean: false);

			sessionRepository.Setup (r => r.Read (It.IsAny<string> ())).Returns (session);

			var unsubscribe = new Unsubscribe (packetId, "foo/bar");

			var channel = new Mock<IMqttChannel<IPacket>> ();

			var response = default(IPacket);

			channel.Setup (c => c.SendAsync (It.IsAny<IPacket> ()))
				.Callback<IPacket> (p => response = p)
				.Returns(Task.Delay(0));

			var connectionProvider = new Mock<IConnectionProvider> ();

			connectionProvider
				.Setup (p => p.GetConnectionAsync (It.Is<string> (c => c == clientId)))
				.Returns (Task.FromResult(channel.Object));

			var flow = new ServerUnsubscribeFlow (sessionRepository.Object);

			await flow.ExecuteAsync(clientId, unsubscribe, channel.Object)
				.ConfigureAwait(continueOnCapturedContext: false);

			sessionRepository.Verify (r => r.Delete (It.IsAny<string> ()), Times.Never);
			Assert.NotNull (response);

			var unsubscribeAck = response as UnsubscribeAck;

			Assert.NotNull (unsubscribeAck);
			Assert.Equal (packetId, unsubscribeAck.PacketId);
		}
	}
}
