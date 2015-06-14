using System;
using Hermes;
using Hermes.Packets;
using IntegrationTests.Context;
using Xunit;

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

		public void Dispose ()
		{
			if (this.server != null) {
				this.server.Stop ();
			}
		}
	}
}
