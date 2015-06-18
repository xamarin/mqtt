using System.Threading.Tasks;
using Hermes;
using IntegrationTests.Context;
using Xunit;
using System.Linq;
using System.Threading;
using System;
using System.Reactive.Linq;
using Hermes.Packets;
using System.Text;
using System.Collections.Generic;

namespace IntegrationTests
{
	public class ConnectionSpec : IntegrationContext, IDisposable
	{
		readonly Server server;

		public ConnectionSpec ()
		{
			this.server = this.GetServer ();
		}

		[Fact]
		public void when_stopping_server_then_it_is_not_reachable()
		{
			this.server.Stop ();

			var ex = Assert.Throws<ClientException>(() => this.GetClient ());

			Assert.NotNull (ex);
			Assert.NotNull (ex.InnerException);
			Assert.True (ex.InnerException is ProtocolException);
		}

		[Fact]
		public async Task when_connect_clients_and_one_client_drops_connection_then_other_client_survives()
		{
			var fooClient = this.GetClient ();
			var barClient = this.GetClient ();

			await fooClient.ConnectAsync (new ClientCredentials (this.GetClientId ()));
			await barClient.ConnectAsync (new ClientCredentials (this.GetClientId ()));

			var exceptionThrown = false;

			try {
				//Force an exception to be thrown by publishing null message
				await fooClient.PublishAsync (message: null, qos: QualityOfService.AtMostOnce);
			} catch {
				exceptionThrown = true;
			}

			var serverSignal = new ManualResetEventSlim ();

			while (!serverSignal.IsSet) {
				if (server.ActiveChannels == 1 && server.ActiveClients.Count () == 1) {
					serverSignal.Set ();
				}
			}

			serverSignal.Wait ();

			Assert.True (exceptionThrown);
			Assert.Equal(1, server.ActiveChannels);
			Assert.Equal(1, server.ActiveClients.Count ());

			fooClient.Close ();
			barClient.Close ();
		}

		[Fact]
		public async Task when_connect_clients_then_succeeds()
		{
			var count = this.GetTestLoad ();
			var clients = new List<IClient> ();
			var tasks = new List<Task> ();

			for (var i = 1; i <= count; i++) {
				var client = this.GetClient ();

				tasks.Add (client.ConnectAsync (new ClientCredentials (this.GetClientId ())));
				clients.Add (client);
			}

			await Task.WhenAll (tasks);

			Assert.Equal (count, server.ActiveClients.Count ());
			Assert.True (clients.All(c => c.IsConnected));
			Assert.True (clients.All(c => !string.IsNullOrEmpty (c.Id)));

			foreach (var client in clients) {
				client.Close ();
			}
		}

		[Fact]
		public async Task when_disconnect_clients_then_succeeds()
		{
			var count = this.GetTestLoad ();
			var clients = new List<IClient> ();
			var connectTasks = new List<Task> ();

			for (var i = 1; i <= count; i++) {
				var client = this.GetClient ();

				connectTasks.Add(client.ConnectAsync (new ClientCredentials (this.GetClientId ())));
				clients.Add (client);
			}

			await Task.WhenAll (connectTasks);

			var disconnectTasks = new List<Task> ();

			foreach (var client in clients) {
				disconnectTasks.Add(client.DisconnectAsync ());
			}

			await Task.WhenAll (disconnectTasks);

			var disconnectedSignal = new ManualResetEventSlim (initialState: false);

			while (!disconnectedSignal.IsSet) {
				if (server.ActiveClients.Count () == 0 && clients.All(c => !c.IsConnected)) {
					disconnectedSignal.Set ();
				}
			}

			Assert.Equal (0, server.ActiveClients.Count ());
			Assert.True (clients.All(c => !c.IsConnected));
			Assert.True (clients.All(c => string.IsNullOrEmpty (c.Id)));

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
			var existClientAfterConnect = server.ActiveClients.Any (c => c == clientId);

			await client.DisconnectAsync ()
				.ConfigureAwait(continueOnCapturedContext: false);

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

			var clientDisconnected = clientClosed.Wait (2000);

			Assert.True (existClientAfterConnect);
			Assert.True (clientDisconnected);
			Assert.False (server.ActiveClients.Any (c => c == clientId));

			client.Close ();
		}

		[Fact]
		public async Task when_client_disconnects_by_protocol_then_will_message_is_not_sent()
		{
			var client1 = this.GetClient ();
			var client2 = this.GetClient ();
			var client3 = this.GetClient ();

			var topic = Guid.NewGuid ().ToString ();
			var qos = QualityOfService.ExactlyOnce;
			var retain = true;
			var message = "Client 1 has been disconnected unexpectedly";
			var will = new Will(topic, qos, retain, message);

			await client1.ConnectAsync (new ClientCredentials (this.GetClientId ()), will);
			await client2.ConnectAsync (new ClientCredentials (this.GetClientId ()));
			await client3.ConnectAsync (new ClientCredentials (this.GetClientId ()));

			await client2.SubscribeAsync(topic, QualityOfService.AtMostOnce);
			await client3.SubscribeAsync(topic, QualityOfService.AtLeastOnce);

			var willReceivedSignal = new ManualResetEventSlim (initialState: false);

			client2.Receiver.Subscribe (m => {
				if (m.Topic == topic) {
					willReceivedSignal.Set ();
				}
			});
			client3.Receiver.Subscribe (m => {
				if (m.Topic == topic) {
					willReceivedSignal.Set ();
				}
			});

			await client1.DisconnectAsync ();

			var willReceived = willReceivedSignal.Wait (2000);

			Assert.False (willReceived);

			client1.Close ();
			client2.Close ();
			client3.Close ();
		}
		
		[Fact]
		public async Task when_client_disconnects_unexpectedly_then_will_message_is_sent()
		{
			var client1 = this.GetClient ();
			var client2 = this.GetClient ();
			var client3 = this.GetClient ();

			var topic = Guid.NewGuid ().ToString ();
			var qos = QualityOfService.ExactlyOnce;
			var retain = true;
			var message = "Client 1 has been disconnected unexpectedly";
			var will = new Will(topic, qos, retain, message);

			await client1.ConnectAsync (new ClientCredentials (this.GetClientId ()), will);
			await client2.ConnectAsync (new ClientCredentials (this.GetClientId ()));
			await client3.ConnectAsync (new ClientCredentials (this.GetClientId ()));

			await client2.SubscribeAsync(topic, QualityOfService.AtMostOnce);
			await client3.SubscribeAsync(topic, QualityOfService.AtLeastOnce);

			var willReceivedSignal = new ManualResetEventSlim (initialState: false);
			var willMessage = default (ApplicationMessage);

			client2.Receiver.Subscribe (m => {
				if (m.Topic == topic) {
					willMessage = m;
					willReceivedSignal.Set ();
				}
			});
			client3.Receiver.Subscribe (m => {
				if (m.Topic == topic) {
					willMessage = m;
					willReceivedSignal.Set ();
				}
			});

			client1.Close ();

			var willReceived = willReceivedSignal.Wait (2000);

			Assert.True (willReceived);
			Assert.NotNull (willMessage);
			Assert.Equal (topic, willMessage.Topic);
			Assert.Equal (message, Encoding.UTF8.GetString (willMessage.Payload));

			client1.Close ();
			client2.Close ();
			client3.Close ();
		}

		public void Dispose ()
		{
			if (this.server != null) {
				this.server.Stop ();
			}
		}
	}
}
