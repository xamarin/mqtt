using System;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
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
		public async Task when_sending_packet_to_existing_client_then_packet_is_forwarded()
		{
			var manager = new ClientManager ();

			var receiver = new Subject<IPacket> ();
			var channel = new Mock<IChannel<IPacket>> ();

			channel.Setup (c => c.Receiver).Returns (receiver);

			var clientId = Guid.NewGuid().ToString();
			var ping = new PingRequest ();

			manager.AddClient (clientId, channel.Object);

			await manager.SendMessageAsync (clientId, ping);

			channel.Verify (c => c.SendAsync (It.Is<IPacket> (p => p is PingRequest)));
		}

		[Fact]
		public void when_sending_packet_to_not_existing_client_then_fails()
		{
			var manager = new ClientManager ();

			var receiver = new Subject<IPacket> ();
			var channel = new Mock<IChannel<IPacket>> ();

			channel.Setup (c => c.Receiver).Returns (receiver);

			var clientId = Guid.NewGuid().ToString();
			var ping = new PingRequest ();

			var ex = Assert.Throws<AggregateException> (() => manager.SendMessageAsync (clientId, ping).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}
	}
}
