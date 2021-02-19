using Moq;
using System;
using System.Linq;
using System.Net.Mqtt;
using System.Net.Mqtt.Sdk;
using System.Net.Mqtt.Sdk.Packets;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
	public class ConnectionProviderSpec
	{
        [Fact]
        public void when_registering_private_client_then_private_client_list_increases()
        {
            var provider = new ConnectionProvider ();

            var existingPrivateClients = provider.PrivateClients.Count ();
            var clientId = Guid.NewGuid ().ToString ();

            provider.RegisterPrivateClient (clientId);

            Assert.Equal (existingPrivateClients + 1, provider.PrivateClients.Count ());
        }

        [Fact]
        public void when_registering_existing_private_client_then_fails()
        {
            var provider = new ConnectionProvider ();

            var clientId = Guid.NewGuid().ToString ();

            provider.RegisterPrivateClient (clientId);

            var ex = Assert.Throws<MqttServerException> (() => provider.RegisterPrivateClient (clientId));

            Assert.NotNull (ex);
        }

        [Fact]
        public async Task when_removing_private_connection_then_private_client_list_decreases()
        {
            var provider = new ConnectionProvider ();

            var clientId = Guid.NewGuid ().ToString ();

            await provider.AddConnectionAsync (clientId, Mock.Of<IMqttChannel<IPacket>> (c => c.IsConnected == true));
            provider.RegisterPrivateClient (clientId);

            var previousPrivateClients = provider.PrivateClients.Count ();

            await provider.RemoveConnectionAsync (clientId);

            var currentPrivateClients = provider.PrivateClients.Count ();

            Assert.Equal (previousPrivateClients - 1, currentPrivateClients);
        }

        [Fact]
		public async Task when_adding_new_client_then_connection_list_increases()
		{
			var provider = new ConnectionProvider ();

			var existingClients = provider.Connections;
			var clientId = Guid.NewGuid ().ToString ();

			await provider.AddConnectionAsync (clientId, Mock.Of<IMqttChannel<IPacket>>(c => c.IsConnected == true)); 

			Assert.Equal (existingClients + 1, provider.Connections);
		}

		[Fact]
		public async Task when_adding_disconnected_client_then_active_clients_list_does_not_increases()
		{
			var provider = new ConnectionProvider ();

			var existingClients = provider.ActiveClients.Count();
			var clientId = Guid.NewGuid ().ToString ();

			await provider.AddConnectionAsync (clientId, Mock.Of<IMqttChannel<IPacket>>(c => c.IsConnected == false)); 

			Assert.Equal (existingClients, provider.ActiveClients.Count());
		}

		[Fact]
		public async Task when_adding_new_client_and_disconnect_it_then_active_clients_list_decreases()
		{
			var provider = new ConnectionProvider ();

			var existingClients = provider.ActiveClients.Count();
			var clientId = Guid.NewGuid ().ToString ();

			var connection = new Mock<IMqttChannel<IPacket>> ();

			connection.Setup(c => c.IsConnected).Returns(true);

			await provider.AddConnectionAsync (clientId, connection.Object);

			var currentClients = provider.ActiveClients.Count();

			connection.Setup (c => c.IsConnected).Returns (false);

			var finalClients = provider.ActiveClients.Count();

			Assert.Equal (existingClients + 1, currentClients);
			Assert.Equal (existingClients, finalClients);
		}

		[Fact]
		public async Task when_removing_clients_then_connection_list_decreases()
		{
			var provider = new ConnectionProvider ();

			var initialClients = provider.Connections;

			var clientId = Guid.NewGuid ().ToString ();

			await provider.AddConnectionAsync (clientId, Mock.Of<IMqttChannel<IPacket>> (c => c.IsConnected == true));

			var newClients = provider.Connections;

			await provider.RemoveConnectionAsync(clientId); 

			var finalClients = provider.Connections;

            Assert.Equal (initialClients + 1, newClients);
            Assert.Equal (initialClients, finalClients);
		}

		[Fact]
		public async Task when_adding_existing_client_id_then_existing_client_is_disconnected()
		{
			var provider = new ConnectionProvider ();

			var receiver1 = new Subject<IPacket> ();
			var channel1 = new Mock<IMqttChannel<IPacket>> ();

			channel1.Setup (c => c.ReceiverStream).Returns (receiver1);
			channel1.Setup (c => c.IsConnected).Returns (true);

			var receiver2 = new Subject<IPacket> ();
			var channel2 = new Mock<IMqttChannel<IPacket>> ();

			channel2.Setup (c => c.ReceiverStream).Returns (receiver2);
			channel2.Setup (c => c.IsConnected).Returns (true);

			var clientId = Guid.NewGuid().ToString();

			await provider.AddConnectionAsync (clientId, channel1.Object);
			await provider.AddConnectionAsync (clientId, channel2.Object);

			channel1.Verify (c => c.CloseAsync ());
			channel2.Verify(c => c.CloseAsync (), Times.Never);
		}

		[Fact]
		public async Task when_getting_connection_from_client_then_succeeds()
		{
			var provider = new ConnectionProvider ();
			var clientId = Guid.NewGuid ().ToString ();

			await provider.AddConnectionAsync (clientId, Mock.Of<IMqttChannel<IPacket>> (c => c.IsConnected == true));

			var connection = await provider.GetConnectionAsync (clientId);

			Assert.NotNull (connection);
		}

		[Fact]
		public async Task when_getting_connection_from_disconnected_client_then_no_connection_is_returned()
		{
			var provider = new ConnectionProvider ();
			var clientId = Guid.NewGuid ().ToString ();

			await provider.AddConnectionAsync (clientId, Mock.Of<IMqttChannel<IPacket>> (c => c.IsConnected == false));

			var connection = await provider.GetConnectionAsync (clientId);

			Assert.Null (connection);
		}
	}
}
