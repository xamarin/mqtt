using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mqtt;
using System.Net.Mqtt.Exceptions;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Server;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IntegrationTests.Context;
using Xunit;

namespace IntegrationTests
{
    public class ConnectionSpec : IntegrationContext, IDisposable
	{
		readonly IMqttServer server;

		public ConnectionSpec ()
		{
			server = GetServerAsync ().Result;
		}

		[Fact]
		public async Task when_stopping_server_then_it_is_not_reachable()
		{
			server.Stop ();

            try {
                await GetClientAsync();
            } catch (Exception ex) {
                Assert.True(ex is MqttException);
            }
		}

		[Fact]
		public async Task when_connect_clients_and_one_client_drops_connection_then_other_client_survives()
		{
			var fooClient = await GetClientAsync ();
			var barClient = await GetClientAsync ();

			await fooClient.ConnectAsync (new MqttClientCredentials (GetClientId ()));
			await barClient.ConnectAsync (new MqttClientCredentials (GetClientId ()));

			var exceptionThrown = false;

			try {
				//Force an exception to be thrown by publishing null message
				await fooClient.PublishAsync (message: null, qos: MqttQualityOfService.AtMostOnce);
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
			var count = GetTestLoad ();
			var clients = new List<IMqttClient> ();
			var tasks = new List<Task> ();

			for (var i = 1; i <= count; i++) {
				var client = await GetClientAsync ();

				tasks.Add (client.ConnectAsync (new MqttClientCredentials (GetClientId ())));
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
			var count = GetTestLoad ();
			var clients = new List<IMqttClient> ();
			var connectTasks = new List<Task> ();

			for (var i = 1; i <= count; i++) {
				var client = await GetClientAsync ();

				connectTasks.Add(client.ConnectAsync (new MqttClientCredentials (GetClientId ())));
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
			var client = await GetClientAsync ();

			await client.ConnectAsync (new MqttClientCredentials (GetClientId ()))
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
			var client1 = await GetClientAsync ();
			var client2 = await GetClientAsync();
            var client3 = await GetClientAsync();

            var topic = Guid.NewGuid ().ToString ();
			var qos = MqttQualityOfService.ExactlyOnce;
			var retain = true;
			var message = "Client 1 has been disconnected unexpectedly";
			var will = new MqttLastWill(topic, qos, retain, message);

			await client1.ConnectAsync (new MqttClientCredentials (GetClientId ()), will);
			await client2.ConnectAsync (new MqttClientCredentials (GetClientId ()));
			await client3.ConnectAsync (new MqttClientCredentials (GetClientId ()));

			await client2.SubscribeAsync(topic, MqttQualityOfService.AtMostOnce);
			await client3.SubscribeAsync(topic, MqttQualityOfService.AtLeastOnce);

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
			var client1 = await GetClientAsync();
            var client2 = await GetClientAsync();
            var client3 = await GetClientAsync();

            var topic = Guid.NewGuid ().ToString ();
			var qos = MqttQualityOfService.ExactlyOnce;
			var retain = true;
			var message = "Client 1 has been disconnected unexpectedly";
			var will = new MqttLastWill(topic, qos, retain, message);

			await client1.ConnectAsync (new MqttClientCredentials (GetClientId ()), will);
			await client2.ConnectAsync (new MqttClientCredentials (GetClientId ()));
			await client3.ConnectAsync (new MqttClientCredentials (GetClientId ()));

			await client2.SubscribeAsync(topic, MqttQualityOfService.AtMostOnce);
			await client3.SubscribeAsync(topic, MqttQualityOfService.AtLeastOnce);

			var willReceivedSignal = new ManualResetEventSlim (initialState: false);
			var willMessage = default (MqttApplicationMessage);

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
			if (server != null) {
				server.Stop ();
			}
		}
	}
}
