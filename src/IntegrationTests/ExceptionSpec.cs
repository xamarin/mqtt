using System;
using System.Net.Mqtt.Client;
using System.Net.Mqtt.Exceptions;
using System.Net.Mqtt.Packets;
using System.Net.Sockets;
using System.Threading.Tasks;
using IntegrationTests.Context;
using Xunit;

namespace IntegrationTests
{
    public class ExceptionSpec : IntegrationContext
    {
        [Fact]
        public void when_connecting_client_to_non_existing_server_then_fails ()
        {
            var aggregateEx = Assert.Throws<AggregateException>(() => GetClientAsync ().Wait ());

            Assert.NotNull (aggregateEx);
            Assert.NotNull (aggregateEx.InnerException);
            Assert.True (aggregateEx.InnerException is ClientException);
            Assert.NotNull (aggregateEx.InnerException.InnerException);
            Assert.True (aggregateEx.InnerException.InnerException is MqttException);
            Assert.NotNull (aggregateEx.InnerException.InnerException.InnerException);
            Assert.True (aggregateEx.InnerException.InnerException.InnerException is SocketException);
        }

        [Fact]
        public async Task when_server_is_closed_then_error_occurs_when_client_send_message ()
        {
            var server = await GetServerAsync ();
            var client = await GetClientAsync ();

            await client.ConnectAsync (new ClientCredentials (GetClientId ()))
                .ConfigureAwait (continueOnCapturedContext: false);

            server.Stop ();

            var aggregateException = Assert.Throws<AggregateException>(() => client.SubscribeAsync ("test\foo", QualityOfService.AtLeastOnce).Wait());

            Assert.NotNull (aggregateException);
            Assert.NotNull (aggregateException.InnerException);
            Assert.True (aggregateException.InnerException is ClientException || aggregateException.InnerException is ObjectDisposedException);
        }
    }
}
