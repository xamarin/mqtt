using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hermes;
using IntegrationTests.Context;
using Xunit;

namespace IntegrationTests
{
	public class ConnectionSpecWithKeepAlive : IntegrationContext, IDisposable
	{
		private readonly Server server;

		public ConnectionSpecWithKeepAlive () 
			: base(keepAliveSecs: 1)
		{
			this.server = this.GetServer ();
		}

		[Fact]
		public async Task when_keep_alive_enabled_and_client_is_disposed_then_server_refresh_active_client_list()
		{
			var client = this.GetClient ();

			await client.ConnectAsync (new ClientCredentials (this.GetClientId ()))
				.ConfigureAwait(continueOnCapturedContext: false);

			var clientId = client.Id;
			var existClientAfterConnect = this.server.ActiveClients.Any (c => c == clientId);
			var clientClosed = new ManualResetEventSlim ();

			var subscription = Observable.Create<bool> (observer => {
				var timer = new System.Timers.Timer();

				timer.Interval = 200;
				timer.Elapsed += (sender, args) => {
					if (this.server.ActiveClients.Any (c => c == clientId)) {
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

			client.Close ();

			var serverDetectedClientClosed = clientClosed.Wait (TimeSpan.FromSeconds(this.keepAliveSecs * 2));

			subscription.Dispose ();

			Assert.True (existClientAfterConnect);
			Assert.True (serverDetectedClientClosed);
			Assert.False (this.server.ActiveClients.Any (c => c == clientId));
		}

		[Fact]
		public async Task when_keep_alive_enabled_and_no_packets_are_sent_then_connection_is_maintained()
		{
			var clients = new List<Client>();
			var count = this.GetTestLoad();

			for (var i = 1; i <= count; i++) {
				var client = this.GetClient ();

				clients.Add (client);
				await client.ConnectAsync (new ClientCredentials (this.GetClientId ()))
					.ConfigureAwait(continueOnCapturedContext: false);
			}

			Thread.Sleep (TimeSpan.FromSeconds(this.keepAliveSecs * 3));

			var connectedClients = clients.Where (c => c.IsConnected);
			var serverClients = clients.Select(c => c.Id).Where(x => this.server.ActiveClients.Any(y => y == x));

			Debug.WriteLine (string.Format ("Connected Clients: {0}", connectedClients.Count ()));
			Debug.WriteLine (string.Format ("Server Clients: {0}", serverClients.Count ()));

			Assert.Equal (clients.Count, connectedClients.Count ());
			Assert.Equal(clients.Count, serverClients.Count());

			foreach (var client in clients) {
				client.Close ();
			}
		}

		public void Dispose ()
		{
			this.server.Stop ();
		}
	}
}
