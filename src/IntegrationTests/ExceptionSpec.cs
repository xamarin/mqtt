using IntegrationTests.Context;
using System;
using System.Net.Mqtt;
using System.Net.Mqtt.Exceptions;
using System.Net.Mqtt.Packets;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace IntegrationTests
{
    public class ExceptionSpec : IntegrationContext
	{
		[Fact]
		public async Task when_connecting_client_to_non_existing_server_then_fails()
		{
            try {
                await GetClientAsync();
            } catch (Exception ex) {
                Assert.True (ex is MqttClientException);
                Assert.NotNull (ex.InnerException);
                Assert.True (ex.InnerException is MqttException);
                Assert.NotNull (ex.InnerException.InnerException);
                Assert.True (ex.InnerException.InnerException is SocketException);
            }
		}

		[Fact]
		public async Task when_server_is_closed_then_error_occurs_when_client_send_message()
		{
			var server = await GetServerAsync ();
			var client = await GetClientAsync ();

			await client.ConnectAsync (new MqttClientCredentials (GetClientId ()))
				.ConfigureAwait(continueOnCapturedContext: false);

			server.Stop ();

			var aggregateException = Assert.Throws<AggregateException>(() => client.SubscribeAsync ("test\foo", MqttQualityOfService.AtLeastOnce).Wait());

			Assert.NotNull (aggregateException);
			Assert.NotNull (aggregateException.InnerException);
			Assert.True (aggregateException.InnerException is MqttClientException || aggregateException.InnerException is ObjectDisposedException);
		}
	}
}
