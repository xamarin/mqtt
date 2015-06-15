using System;
using System.Threading.Tasks;
using System.Net.Mqtt;
using System.Net.Mqtt.Packets;
using IntegrationTests.Context;
using Xunit;
using System.Net.Mqtt.Server;
using System.Net.Mqtt.Client;

namespace IntegrationTests
{
	public class AuthenticationSpec : IntegrationContext, IDisposable
	{
		readonly Server server;

		public AuthenticationSpec ()
		{
			this.server = this.GetServer (new TestAuthenticationProvider(expectedUsername: "foo", expectedPassword: "foo123"));
		}

		[Fact]
		public void when_client_connects_with_invalid_credentials_and_authentication_is_supported_then_connection_is_closed()
		{
			var username = "foo";
			var password = "foo123456";
			var client = this.GetClient ();

			var aggregateEx = Assert.Throws<AggregateException>(() => client.ConnectAsync (new ClientCredentials (this.GetClientId (), username, password)).Wait());

			Assert.NotNull (aggregateEx.InnerException);
			Assert.True (aggregateEx.InnerException is ClientException);
			Assert.NotNull (aggregateEx.InnerException.InnerException);
			Assert.True (aggregateEx.InnerException.InnerException is ProtocolConnectionException);
			Assert.Equal (ConnectionStatus.BadUserNameOrPassword, ((ProtocolConnectionException)aggregateEx.InnerException.InnerException).ReturnCode);
		}

		[Fact]
		public async Task when_client_connects_with_valid_credentials_and_authentication_is_supported_then_connection_succeeds()
		{
			var username = "foo";
			var password = "foo123";
			var client = this.GetClient ();

			await client.ConnectAsync (new ClientCredentials (this.GetClientId (), username, password));

			Assert.True(client.IsConnected);
			Assert.False(string.IsNullOrEmpty(client.Id));
		}

		public void Dispose ()
		{
			if (this.server != null) {
				this.server.Stop ();
			}
		}
	}
}
