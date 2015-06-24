using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Net.Mqtt;
using System.Net.Mqtt.Packets;
using IntegrationTests.Context;
using Xunit;
using System.Net.Mqtt.Client;

namespace IntegrationTests
{
	public class ExceptionSpec : IntegrationContext
	{
		[Fact]
		public void when_connecting_client_to_non_existing_server_then_fails()
		{
			var clientException = Assert.Throws<ClientException>(() => this.GetClient ());

			Assert.NotNull (clientException);
			Assert.NotNull (clientException.InnerException);
			Assert.NotNull (clientException.InnerException.InnerException);
			Assert.True (clientException.InnerException is ProtocolException);
			Assert.True (clientException.InnerException.InnerException is SocketException);
		}

		[Fact]
		public async Task when_server_is_closed_then_error_occurs_when_client_send_message()
		{
			var server = this.GetServer ();
			var client = this.GetClient ();

			await client.ConnectAsync (new ClientCredentials(this.GetClientId()))
				.ConfigureAwait(continueOnCapturedContext: false);

			server.Stop ();

			var aggregateException = Assert.Throws<AggregateException>(() => client.SubscribeAsync ("test\foo", QualityOfService.AtLeastOnce).Wait());

			Assert.NotNull (aggregateException);
			Assert.NotNull (aggregateException.InnerException);
			Assert.True (aggregateException.InnerException is ClientException || aggregateException.InnerException is ObjectDisposedException);
		}
	}
}
