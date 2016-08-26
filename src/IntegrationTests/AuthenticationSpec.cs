using System;
using System.Net.Mqtt.Server;
using System.Threading.Tasks;
using IntegrationTests.Context;
using Xunit;

namespace IntegrationTests
{
    public class AuthenticationSpec : IntegrationContext, IDisposable
	{
		readonly IMqttServer server;

		public AuthenticationSpec ()
		{
			server = GetServer (new TestAuthenticationProvider(expectedUsername: "foo", expectedPassword: "foo123"));
		}

		[Fact]
		public void when_client_connects_with_invalid_credentials_and_authentication_is_supported_then_connection_is_closed()
		{
			var username = "foo";
			var password = "foo123456";
			var client = GetClient ();

			var aggregateEx = Assert.Throws<AggregateException>(() => client.ConnectAsync (new ClientCredentials (GetClientId (), username, password)).Wait());

			Assert.NotNull (aggregateEx.InnerException);
			Assert.True (aggregateEx.InnerException is MqttClientException);
			Assert.NotNull (aggregateEx.InnerException.InnerException);
			Assert.True (aggregateEx.InnerException.InnerException is MqttConnectionException);
			Assert.Equal (MqttConnectionStatus.BadUserNameOrPassword, ((MqttConnectionException)aggregateEx.InnerException.InnerException).ReturnCode);
		}

		[Fact]
		public async Task when_client_connects_with_valid_credentials_and_authentication_is_supported_then_connection_succeeds()
		{
			var username = "foo";
			var password = "foo123";
			var client = GetClient ();

			await client.ConnectAsync (new ClientCredentials (GetClientId (), username, password));

			Assert.True(client.IsConnected);
			Assert.False(string.IsNullOrEmpty(client.Id));
		}

		public void Dispose ()
		{
			if (server != null) {
				server.Stop ();
			}
		}
	}
}
