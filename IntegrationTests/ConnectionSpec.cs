using System.Threading.Tasks;
using Hermes;
using IntegrationTests.Context;
using Xunit;
using System.Linq;
using System.Threading;
using System;
using System.Reactive.Linq;

namespace IntegrationTests
{
	public class ConnectionSpec : IntegrationContext
	{
		protected readonly ushort keepAliveSecs;

		public ConnectionSpec ()
			: this(keepAliveSecs: 0)
		{
		}

		public ConnectionSpec (ushort keepAliveSecs)
			: base(keepAliveSecs: keepAliveSecs)
		{
			this.keepAliveSecs = keepAliveSecs;
		}

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

				timer.Interval = 1000;
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
				x => { Console.WriteLine (string.Format("Client closed: {0}", x)); },
				ex => { Console.WriteLine (string.Format ("Error: {0}", ex.Message)); });

			client.Close ();

			var clientDisconnected = clientClosed.Wait (TimeSpan.FromSeconds(2));

			Assert.True (existClientAfterConnect);
			Assert.True (clientDisconnected);
		}
	}

	public class ConnectionSpecWithKeepAlive : ConnectionSpec
	{
		public ConnectionSpecWithKeepAlive () 
			: base(keepAliveSecs: 1)
		{
		}

		[Fact]
		public async Task when_keep_alive_enabled_and_client_is_disposed_then_server_refresh_active_client_list()
		{
			var client = this.GetClient ();

			await client.ConnectAsync (new ClientCredentials (this.GetClientId ()));

			var clientId = client.Id;
			var existClientAfterConnect = this.fixture.Server.ActiveClients.Any (c => c == clientId);
			var clientClosed = new ManualResetEventSlim ();

			var subscription = Observable.Create<bool> (observer => {
				var timer = new System.Timers.Timer();

				timer.Interval = 1000;
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
				x => { Console.WriteLine (string.Format("Client closed: {0}", x)); },
				ex => { Console.WriteLine (string.Format ("Error: {0}", ex.Message)); });

			client.Close ();

			var serverDetectedClientClosed = clientClosed.Wait (TimeSpan.FromSeconds(this.keepAliveSecs * 3));

			subscription.Dispose ();

			Assert.True (existClientAfterConnect);
			Assert.True (serverDetectedClientClosed);
		}
	}

	public class ConnectionSpecWithNoKeepAlive : ConnectionSpec
	{
		public ConnectionSpecWithNoKeepAlive ()
			: base()
		{
		}

		[Fact]
		public async Task when_keep_alive_not_enabled_and_client_is_disposed_then_server_does_not_refresh_active_client_list()
		{
			var client = this.GetClient ();

			await client.ConnectAsync (new ClientCredentials (this.GetClientId ()));

			var clientId = client.Id;
			var existClientAfterConnect = this.fixture.Server.ActiveClients.Any (c => c == clientId);
			var clientClosed = new ManualResetEventSlim ();

			var subscription = Observable.Create<bool> (observer => {
				var timer = new System.Timers.Timer();

				timer.Interval = 500;
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
				x => { Console.WriteLine (string.Format("Client closed: {0}", x)); },
				ex => { Console.WriteLine (string.Format ("Error: {0}", ex.Message)); });

			client.Close ();

			var serverDetectedClientClosed = clientClosed.Wait (TimeSpan.FromSeconds(2));

			subscription.Dispose ();

			Assert.True (existClientAfterConnect);
			Assert.False (serverDetectedClientClosed);
		}
	}
}
