using System.Threading.Tasks;
using Hermes;
using IntegrationTests.Context;
using Xunit;
using System.Linq;
using System.Threading;
using System;
using System.Reactive.Linq;
using System.Collections.Generic;
using Hermes.Packets;

namespace IntegrationTests
{
	public class ConnectionSpec : IntegrationContext, IDisposable
	{
		private readonly Server server;

		public ConnectionSpec ()
			: base()
		{
			this.server = this.GetServer ();
		}

		[Fact]
		public async Task when_connect_clients_and_one_client_drops_connection_then_other_client_survives()
		{
			var fooClient = this.GetClient ();
			var barClient = this.GetClient ();

			await fooClient.ConnectAsync (new ClientCredentials (this.GetClientId ()));
			await barClient.ConnectAsync (new ClientCredentials (this.GetClientId ()));

			var exceptionThrown = false;
			var serverSignal = new ManualResetEventSlim ();
			var serverTimer = new System.Timers.Timer ();

			serverTimer.Interval = 100;
			serverTimer.AutoReset = true;
			serverTimer.Elapsed += (sender, e) => {
				if (this.server.ActiveChannels == 1 && server.ActiveClients.Count() == 1) {
					serverSignal.Set ();
					serverTimer.Stop ();
				}
			};

			serverTimer.Start ();

			try {
				//Force an exception to be thrown by publishing null message
				await fooClient.PublishAsync (message: null, qos: QualityOfService.AtMostOnce);
			} catch {
				exceptionThrown = true;
			}
			
			var topic = "foo/#";

			await barClient.SubscribeAsync (topic, QualityOfService.AtMostOnce);

			var serverNotified = serverSignal.Wait (TimeSpan.FromSeconds (1));

			Assert.True (serverNotified);
			Assert.True (exceptionThrown);
			Assert.Equal(1, this.server.ActiveChannels);
			Assert.Equal(1, this.server.ActiveClients.Count ());

			serverTimer.Dispose ();
		}

		[Fact]
		public async Task when_connect_clients_then_succeeds()
		{
			var clients = new List<Client>();
			var count = this.GetTestLoad();

			for (var i = 1; i <= count; i++) {
				var client = this.GetClient ();

				clients.Add (client);
				await client.ConnectAsync (new ClientCredentials (this.GetClientId ()))
					.ConfigureAwait(continueOnCapturedContext: false);
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

				clients.Add (client);
				await client.ConnectAsync (new ClientCredentials (this.GetClientId ()))
					.ConfigureAwait(continueOnCapturedContext: false);
			}

			foreach (var client in clients) {
				await client.DisconnectAsync ()
					.ConfigureAwait(continueOnCapturedContext: false);
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

			await client.ConnectAsync (new ClientCredentials (this.GetClientId ()))
				.ConfigureAwait(continueOnCapturedContext: false);

			var clientId = client.Id;
			var existClientAfterConnect = this.server.ActiveClients.Any (c => c == clientId);

			await client.DisconnectAsync ()
				.ConfigureAwait(continueOnCapturedContext: false);

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

			var clientDisconnected = clientClosed.Wait (TimeSpan.FromSeconds(1));

			Assert.True (existClientAfterConnect);
			Assert.True (clientDisconnected);
			Assert.False (this.server.ActiveClients.Any (c => c == clientId));

			client.Close ();
		}

		public void Dispose ()
		{
			this.server.Stop ();
		}
	}
}
