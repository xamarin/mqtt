using System;
using System.IO;
using System.Net.Mqtt;
using System.Threading.Tasks;
using IntegrationTests.Context;
using Xunit;

namespace IntegrationTests
{
	public class AuthenticationSpec : IntegrationContext, IDisposable
	{
		readonly IMqttServer server;
		readonly string Expected;

		public AuthenticationSpec()
		{
			Expected = Path.GetTempFileName();
			server = GetServerAsync(new TestAuthenticationProvider(expectedUsername: "foo", expectedPassword: Expected)).Result;
		}

		[Fact]
		public async Task when_client_connects_with_invalid_credentials_and_authentication_is_supported_then_connection_is_closed()
		{
			var username = "foo";
			var password = "bar" + Expected;
			var client = await GetClientAsync();

			var aggregateEx = Assert.Throws<AggregateException>(() => client.ConnectAsync(new MqttClientCredentials(GetClientId(), username, password)).Wait());

			Assert.NotNull(aggregateEx.InnerException);
			Assert.True(aggregateEx.InnerException is MqttClientException);
			Assert.NotNull(aggregateEx.InnerException.InnerException);
			Assert.True(aggregateEx.InnerException.InnerException is MqttConnectionException);
			Assert.Equal(MqttConnectionStatus.BadUserNameOrPassword, ((MqttConnectionException)aggregateEx.InnerException.InnerException).ReturnCode);
		}

		[Fact]
		public async Task when_client_connects_with_valid_credentials_and_authentication_is_supported_then_connection_succeeds()
		{
			var username = "foo";
			var password = Expected;
			var client = await GetClientAsync();

			await client.ConnectAsync(new MqttClientCredentials(GetClientId(), username, password));

			Assert.True(client.IsConnected);
			Assert.False(string.IsNullOrEmpty(client.Id));
		}

		public void Dispose()
		{
			server?.Stop();
		}
	}
}
