using System;
using System.Linq;
using System.Reactive.Subjects;
using Hermes;
using Hermes.Packets;
using Moq;
using Xunit;

namespace Tests
{
	public class ClientManagerSpec
	{
		[Fact]
		public void when_adding_new_client_then_client_list_increases()
		{
			var manager = new ClientManager ();

			var existingClients = manager.Clients.Count ();
			var clientId = Guid.NewGuid ().ToString ();

			manager.AddClient (clientId, Mock.Of<IChannel<IPacket>> ());

			Assert.Equal (existingClients + 1, manager.Clients.Count ());
			Assert.True (manager.Clients.Any (c => c == clientId));
		}

		[Fact]
		public void when_removing_clients_then_client_list_decreases()
		{
			var manager = new ClientManager ();

			var initialClients = manager.Clients.Count ();

			var clientId = Guid.NewGuid ().ToString ();

			manager.AddClient (clientId, Mock.Of<IChannel<IPacket>> ());

			var newClients = manager.Clients.Count ();

			manager.RemoveClient (clientId);

			var finalClients = manager.Clients.Count ();

			Assert.Equal (initialClients, finalClients);
		}

		[Fact]
		public void when_adding_existing_client_id_then_existing_client_is_disconnected()
		{
			var manager = new ClientManager ();

			var receiver1 = new Subject<IPacket> ();
			var channel1 = new Mock<IChannel<IPacket>> ();

			channel1.Setup (c => c.Receiver).Returns (receiver1);

			var receiver2 = new Subject<IPacket> ();
			var channel2 = new Mock<IChannel<IPacket>> ();

			channel2.Setup (c => c.Receiver).Returns (receiver2);

			var clientId = Guid.NewGuid().ToString();

			manager.AddClient (clientId, channel1.Object);
			manager.AddClient (clientId, channel2.Object);

			channel1.Verify (c => c.Close ());
			channel2.Verify(c => c.Close(), Times.Never);
		}

		[Fact]
		public void when_removing_not_existing_client_then_fail()
		{
			var manager = new ClientManager ();
			var clientId = Guid.NewGuid ().ToString ();

			Assert.Throws<ProtocolException>(() => manager.RemoveClient (clientId));
		}

		[Fact]
		public void when_getting_connection_from_client_then_succeeds()
		{
			var manager = new ClientManager ();
			var clientId = Guid.NewGuid ().ToString ();

			manager.AddClient (clientId, Mock.Of<IChannel<IPacket>> ());

			var connection = manager.GetConnection (clientId);

			Assert.NotNull (connection);
		}

		[Fact]
		public void when_getting_connection_from_not_existing_client_then_fail()
		{
			var manager = new ClientManager ();
			var clientId = Guid.NewGuid ().ToString ();
			
			Assert.Throws<ProtocolException>(() => manager.GetConnection (clientId));
		}
	}
}
