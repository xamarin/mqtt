using System;
using System.Linq;
using System.Reactive.Subjects;
using System.Net.Mqtt;
using System.Net.Mqtt.Packets;
using Moq;
using Xunit;

namespace Tests
{
	public class ConnectionProviderSpec
	{
		[Fact]
		public void when_adding_new_client_then_connection_list_increases()
		{
			var provider = new ConnectionProvider ();

			var existingClients = provider.Connections;
			var clientId = Guid.NewGuid ().ToString ();

			provider.AddConnection (clientId, Mock.Of<IChannel<IPacket>> (c => c.IsConnected == true));

			Assert.Equal (existingClients + 1, provider.Connections);
		}

		[Fact]
		public void when_adding_disconnected_client_then_active_clients_list_does_not_increase()
		{
			var provider = new ConnectionProvider ();

			var existingClients = provider.ActiveClients.Count();
			var clientId = Guid.NewGuid ().ToString ();

			provider.AddConnection (clientId, Mock.Of<IChannel<IPacket>> (c => c.IsConnected == false));

			Assert.Equal (existingClients, provider.ActiveClients.Count());
		}

		[Fact]
		public void when_adding_new_client_and_disconnect_it_then_active_clients_list_decreases()
		{
			var provider = new ConnectionProvider ();

			var existingClients = provider.ActiveClients.Count();
			var clientId = Guid.NewGuid ().ToString ();

			var connection = new Mock<IChannel<IPacket>> ();

			connection.Setup(c => c.IsConnected).Returns(true);

			provider.AddConnection (clientId, connection.Object);

			var currentClients = provider.ActiveClients.Count();

			connection.Setup (c => c.IsConnected).Returns (false);

			var finalClients = provider.ActiveClients.Count();

			Assert.Equal (existingClients + 1, currentClients);
			Assert.Equal (existingClients, finalClients);
		}

		[Fact]
		public void when_removing_clients_then_connection_list_decreases()
		{
			var provider = new ConnectionProvider ();

			var initialClients = provider.Connections;

			var clientId = Guid.NewGuid ().ToString ();

			provider.AddConnection (clientId, Mock.Of<IChannel<IPacket>> (c => c.IsConnected == true));

			var newClients = provider.Connections;

			provider.RemoveConnection (clientId);

			var finalClients = provider.Connections;

			Assert.Equal (initialClients, finalClients);
		}

		[Fact]
		public void when_adding_existing_client_id_then_existing_client_is_disconnected()
		{
			var provider = new ConnectionProvider ();

			var receiver1 = new Subject<IPacket> ();
			var channel1 = new Mock<IChannel<IPacket>> ();

			channel1.Setup (c => c.Receiver).Returns (receiver1);
			channel1.Setup (c => c.IsConnected).Returns (true);

			var receiver2 = new Subject<IPacket> ();
			var channel2 = new Mock<IChannel<IPacket>> ();

			channel2.Setup (c => c.Receiver).Returns (receiver2);
			channel2.Setup (c => c.IsConnected).Returns (true);

			var clientId = Guid.NewGuid().ToString();

			provider.AddConnection (clientId, channel1.Object);
			provider.AddConnection (clientId, channel2.Object);

			channel1.Verify (c => c.Dispose ());
			channel2.Verify(c => c.Dispose(), Times.Never);
		}

		[Fact]
		public void when_getting_connection_from_client_then_succeeds()
		{
			var provider = new ConnectionProvider ();
			var clientId = Guid.NewGuid ().ToString ();

			provider.AddConnection (clientId, Mock.Of<IChannel<IPacket>> (c => c.IsConnected == true));

			var connection = provider.GetConnection (clientId);

			Assert.NotNull (connection);
		}

		[Fact]
		public void when_getting_connection_from_disconnected_client_then_no_connection_is_returned()
		{
			var provider = new ConnectionProvider ();
			var clientId = Guid.NewGuid ().ToString ();

			provider.AddConnection (clientId, Mock.Of<IChannel<IPacket>> (c => c.IsConnected == false));

			var connection = provider.GetConnection (clientId);

			Assert.Null (connection);
		}
	}
}
