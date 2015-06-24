using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using IntegrationTests.Context;
using Xunit;
using System.Net.Mqtt.Server;
using System.Net.Mqtt.Client;

namespace IntegrationTests
{
	public class ConnectionSpecWithKeepAlive : IntegrationContext, IDisposable
	{
		readonly Server server;

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
			var existClientAfterConnect = server.ActiveClients.Any (c => c == clientId);
			var clientClosed = new ManualResetEventSlim ();

			var subscription = Observable.Create<bool> (observer => {
				var timer = new System.Timers.Timer();

				timer.Interval = 200;
				timer.Elapsed += (sender, args) => {
					if (server.ActiveClients.Any (c => c == clientId)) {
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
			Assert.False (server.ActiveClients.Any (c => c == clientId));
		}

		[Fact]
		public async Task when_keep_alive_enabled_and_no_packets_are_sent_then_connection_is_maintained()
		{
			var client = this.GetClient ();

			await client.ConnectAsync (new ClientCredentials (this.GetClientId ()))
				.ConfigureAwait(continueOnCapturedContext: false);

			Thread.Sleep (TimeSpan.FromSeconds(this.keepAliveSecs * 5));

			Assert.Equal (1, server.ActiveClients.Count ());
			Assert.True(client.IsConnected);
			Assert.False (string.IsNullOrEmpty (client.Id));

			client.Close ();
		}

		public void Dispose ()
		{
			if (this.server != null) {
				this.server.Stop ();
			}
		}
	}
}
