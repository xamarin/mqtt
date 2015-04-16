using System.Threading.Tasks;
using Hermes;
using IntegrationTests.Context;
using Xunit;
using System.Linq;
using System.Threading;
using System;
using System.Reactive.Linq;
using System.Collections.Generic;

namespace IntegrationTests
{
	public class ConnectionSpec : IntegrationContext
	{
		public ConnectionSpec ()
			: base()
		{
		}

		[Fact]
		public async Task when_connect_clients_then_succeeds()
		{
			var clients = new List<Client>();
			var count = this.GetTestLoad();

			for (var i = 1; i <= count; i++) {
				var client = this.GetClient ();

				await client.ConnectAsync (new ClientCredentials (this.GetClientId()));

				clients.Add (client);
			}

			Assert.True (clients.All(c => c.IsConnected));
			Assert.True (clients.All (c => !string.IsNullOrEmpty (c.Id)));

			foreach (var client in clients) {
				client.Close ();
			}
		}

		[Fact]
		public async Task when_disconnect_client_then_succeeds()
		{
			var clients = new List<Client>();
			var count = this.GetTestLoad();

			for (var i = 1; i <= count; i++) {
				var client = this.GetClient ();

				await client.ConnectAsync (new ClientCredentials (this.GetClientId()));
				await client.DisconnectAsync ();

				clients.Add (client);
			}

			Assert.True (clients.All(c => !c.IsConnected));
			Assert.True (clients.All (c => string.IsNullOrEmpty (c.Id)));

			foreach (var client in clients) {
				client.Close ();
			}
		}

		[Fact]
		public async Task when_disconnect_client_then_server_decrease_active_client_list()
		{
			var client = this.GetClient ();

			await client.ConnectAsync (new ClientCredentials (this.GetClientId ()));

			var clientId = client.Id;
			var existClientAfterConnect = this.fixture.Server.ActiveClients.Any (c => c == clientId);

			await client.DisconnectAsync ();

			var clientClosed = new ManualResetEventSlim ();

			var subscription = Observable.Create<bool> (observer => {
				var timer = new System.Timers.Timer();

				timer.Interval = 200;
				timer.Elapsed += (sender, args) => {
					if (this.fixture.Server.ActiveClients.Any (c => c == clientId)) {
						observer.OnNext (false);
					} else {
						observer.OnNext (true);
						clientClosed.Set ();
						observer.OnCompleted ();
					}
				};
				timer.Start();

				return () => {
					timer.Dispose();
				};
			})
			.Subscribe (
				_ => { },
				ex => { Console.WriteLine (string.Format ("Error: {0}", ex.Message)); });

			var clientDisconnected = clientClosed.Wait (TimeSpan.FromSeconds(4));

			Assert.True (existClientAfterConnect);
			Assert.True (clientDisconnected);
			Assert.False (this.fixture.Server.ActiveClients.Any (c => c == clientId));

			client.Close ();
		}
	}
}
