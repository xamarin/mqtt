using System;
using System.Net.Mqtt;
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
		public void when_connecting_client_to_non_existing_server_then_fails()
		{
			var clientException = Assert.Throws<MqttClientException>(() => GetClient ());

			Assert.NotNull (clientException);
			Assert.NotNull (clientException.InnerException);
			Assert.NotNull (clientException.InnerException.InnerException);
			Assert.True (clientException.InnerException is MqttException);
			Assert.True (clientException.InnerException.InnerException is SocketException);
		}

		[Fact]
		public async Task when_server_is_closed_then_error_occurs_when_client_send_message()
		{
			var server = GetServer ();
			var client = GetClient ();

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
