using System.Threading.Tasks;
using Hermes;
using IntegrationTests.Context;
using Xunit;

namespace IntegrationTests
{
	public class ConnectionSpec : IntegrationContext
	{
		[Fact]
		public async Task when_connect_clients_then_succeeds()
		{
			var client1 = this.GetClient ();
			var client2 = this.GetClient ();

			await client1.ConnectAsync (new ClientCredentials (this.GetClientId()));
			await client2.ConnectAsync (new ClientCredentials (this.GetClientId()));

			Assert.True (client1.IsConnected);
			Assert.False (string.IsNullOrEmpty (client1.Id));
			Assert.True (client2.IsConnected);
			Assert.False (string.IsNullOrEmpty (client2.Id));

			client1.Close ();
			client2.Close ();
		}

		[Fact]
		public async Task when_disconnect_client_then_succeeds()
		{
			var client1 =  this.GetClient ();
			var	client2 = this.GetClient ();

			await client1.ConnectAsync (new ClientCredentials (this.GetClientId()));
			await client2.ConnectAsync (new ClientCredentials (this.GetClientId()));

			await client1.DisconnectAsync();
			await client2.DisconnectAsync();

			Assert.False (client1.IsConnected);
			Assert.True (string.IsNullOrEmpty (client1.Id));
			Assert.False (client2.IsConnected);
			Assert.True (string.IsNullOrEmpty (client2.Id));

			client1.Close ();
			client2.Close ();
		}
	}
}
