using IntegrationTests.Context;
using IntegrationTests.Messages;
using System;
using System.Linq;
using System.Net.Mqtt;
using System.Threading.Tasks;
using Xunit;

namespace IntegrationTests
{
	public class PrivateClientSpec : IntegrationContext, IDisposable
    {
        readonly IMqttServer server;

        public PrivateClientSpec()
        {
            server = GetServerAsync ().Result;
        }

        [Fact]
        public async Task when_creating_in_process_client_then_it_is_already_connected()
        {
            var client = await server.CreateClientAsync ();

            Assert.NotNull (client);
            Assert.True (client.IsConnected);
            Assert.False (string.IsNullOrEmpty (client.Id));
            Assert.StartsWith ("private", client.Id);

            client.Dispose ();
        }

        [Fact]
        public async Task when_in_process_client_subscribe_to_topic_then_succeeds()
        {
            var client = await server.CreateClientAsync ();
            var topicFilter = Guid.NewGuid ().ToString () + "/#";

            await client.SubscribeAsync (topicFilter, MqttQualityOfService.AtMostOnce)
                .ConfigureAwait (continueOnCapturedContext: false);

            Assert.True (client.IsConnected);

            await client.UnsubscribeAsync (topicFilter);

            client.Dispose();
        }

        [Fact]
        public async Task when_in_process_client_subscribe_to_system_topic_then_succeeds()
        {
            var client = await server.CreateClientAsync ();
            var topicFilter = "$SYS/" + Guid.NewGuid ().ToString () + "/#";

            await client.SubscribeAsync (topicFilter, MqttQualityOfService.AtMostOnce)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.True (client.IsConnected);

            await client.UnsubscribeAsync (topicFilter);

            client.Dispose ();
        }

        [Fact]
        public async Task when_in_process_client_publish_messages_then_succeeds()
        {
            var client = await server.CreateClientAsync ();
            var topic = Guid.NewGuid ().ToString ();
            var testMessage = new TestMessage
            {
                Name = string.Concat ("Message ", Guid.NewGuid ().ToString ().Substring (0, 4)),
                Value = new Random ().Next ()
            };
            var message = new MqttApplicationMessage (topic, Serializer.Serialize(testMessage));

            await client.PublishAsync (message, MqttQualityOfService.AtMostOnce);
            await client.PublishAsync (message, MqttQualityOfService.AtLeastOnce);
            await client.PublishAsync (message, MqttQualityOfService.ExactlyOnce);

            Assert.True (client.IsConnected);

            client.Dispose ();
        }

        [Fact]
        public async Task when_in_process_client_publish_system_messages_then_succeeds()
        {
            var client = await server.CreateClientAsync ();
            var topic = "$SYS/" + Guid.NewGuid ().ToString ();
            var testMessage = new TestMessage
            {
                Name = string.Concat ("Message ", Guid.NewGuid ().ToString ().Substring (0, 4)),
                Value = new Random ().Next ()
            };
            var message = new MqttApplicationMessage (topic, Serializer.Serialize (testMessage));

            await client.PublishAsync (message, MqttQualityOfService.AtMostOnce);
            await client.PublishAsync (message, MqttQualityOfService.AtLeastOnce);
            await client.PublishAsync (message, MqttQualityOfService.ExactlyOnce);

            Assert.True (client.IsConnected);

            client.Dispose ();
        }

        [Fact]
        public async Task when_in_process_client_disconnect_then_succeeds()
        {
            var client = await server.CreateClientAsync ();
			var clientId = client.Id;

            await client.DisconnectAsync ();

            Assert.DoesNotContain (server.ActiveClients, c => c == clientId);
            Assert.False (client.IsConnected);
            Assert.True (string.IsNullOrEmpty (client.Id));

            client.Dispose ();
        }

        [Fact]
        public async Task when_in_process_clients_communicate_each_other_then_succeeds()
        {
            var fooClient = await server.CreateClientAsync ();
            var barClient = await server.CreateClientAsync ();
            var fooTopic = "foo/message";

            await fooClient.SubscribeAsync (fooTopic, MqttQualityOfService.ExactlyOnce);

            var messagesReceived = 0;

            fooClient.MessageStream.Subscribe (message => {
                if (message.Topic == fooTopic) {
                    messagesReceived++;
                }
            });

            await barClient.PublishAsync (new MqttApplicationMessage (fooTopic, new byte[255]), MqttQualityOfService.AtMostOnce);
            await barClient.PublishAsync (new MqttApplicationMessage (fooTopic, new byte[10]), MqttQualityOfService.AtLeastOnce);
            await barClient.PublishAsync (new MqttApplicationMessage ("other/topic", new byte[500]), MqttQualityOfService.ExactlyOnce);
            await barClient.PublishAsync (new MqttApplicationMessage (fooTopic, new byte[50]), MqttQualityOfService.ExactlyOnce);

            await Task.Delay (TimeSpan.FromMilliseconds (1000));

            Assert.True (fooClient.IsConnected);
            Assert.True (barClient.IsConnected);
            Assert.Equal (3, messagesReceived);

            fooClient.Dispose ();
            barClient.Dispose ();
        }

        [Fact]
        public async Task when_in_process_client_communicate_with_tcp_client_then_succeeds()
        {
            var inProcessClient = await server.CreateClientAsync ();
            var remoteClient = await GetClientAsync ();

            await remoteClient.ConnectAsync (new MqttClientCredentials (GetClientId ()));

            var fooTopic = "foo/message";
            var barTopic = "bar/message";

            await inProcessClient.SubscribeAsync (fooTopic, MqttQualityOfService.ExactlyOnce);
            await remoteClient.SubscribeAsync (barTopic, MqttQualityOfService.AtLeastOnce);

            var fooMessagesReceived = 0;
            var barMessagesReceived = 0;

            inProcessClient.MessageStream.Subscribe (message => {
                if (message.Topic == fooTopic)
                {
                    fooMessagesReceived++;
                }
            });
            remoteClient.MessageStream.Subscribe (message => {
                if (message.Topic == barTopic)
                {
                    barMessagesReceived++;
                }
            });

            await remoteClient.PublishAsync (new MqttApplicationMessage (fooTopic, new byte[255]), MqttQualityOfService.AtMostOnce);
            await remoteClient.PublishAsync (new MqttApplicationMessage (fooTopic, new byte[10]), MqttQualityOfService.AtLeastOnce);
            await remoteClient.PublishAsync (new MqttApplicationMessage ("other/topic", new byte[500]), MqttQualityOfService.ExactlyOnce);
            await remoteClient.PublishAsync (new MqttApplicationMessage (fooTopic, new byte[50]), MqttQualityOfService.ExactlyOnce);

            await inProcessClient.PublishAsync (new MqttApplicationMessage (barTopic, new byte[255]), MqttQualityOfService.AtMostOnce);
            await inProcessClient.PublishAsync (new MqttApplicationMessage (barTopic, new byte[10]), MqttQualityOfService.AtLeastOnce);
            await inProcessClient.PublishAsync (new MqttApplicationMessage ("other/topic", new byte[500]), MqttQualityOfService.ExactlyOnce);
            await inProcessClient.PublishAsync (new MqttApplicationMessage (barTopic, new byte[50]), MqttQualityOfService.ExactlyOnce);

            await Task.Delay(TimeSpan.FromMilliseconds(1000));

            Assert.True (inProcessClient.IsConnected);
            Assert.True (remoteClient.IsConnected);
            Assert.Equal (3, fooMessagesReceived);
            Assert.Equal (3, barMessagesReceived);

            inProcessClient.Dispose ();
            remoteClient.Dispose ();
        }

        public void Dispose()
        {
            server?.Stop();
        }
    }
}
