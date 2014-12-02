using System;
using System.Reactive.Subjects;
using Hermes;
using Hermes.Packets;
using Moq;
using Xunit;

namespace Tests
{
	public class ConnectionProviderSpec
	{
		[Fact]
		public void when_adding_new_client_then_client_list_increases()
		{
			var provider = new ConnectionProvider ();

			var existingClients = provider.Connections;
			var clientId = Guid.NewGuid ().ToString ();

			provider.AddConnection (clientId, Mock.Of<IChannel<IPacket>> (c => c.IsConnected == true));

			Assert.Equal (existingClients + 1, provider.Connections);
		}

		[Fact]
		public void when_removing_clients_then_client_list_decreases()
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

			channel1.Verify (c => c.Close ());
			channel2.Verify(c => c.Close(), Times.Never);
		}

		[Fact(Skip = "Revert changes on ConnectionProvider")]
		public void when_removing_not_existing_client_then_fail()
		{
			var provider = new ConnectionProvider ();
			var clientId = Guid.NewGuid ().ToString ();

			Assert.Throws<ProtocolException>(() => provider.RemoveConnection (clientId));
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
		public void when_getting_connection_from_not_existing_client_then_fail()
		{
			var provider = new ConnectionProvider ();
			var clientId = Guid.NewGuid ().ToString ();
			
			Assert.Throws<ProtocolException>(() => provider.GetConnection (clientId));
		}
	}
}
