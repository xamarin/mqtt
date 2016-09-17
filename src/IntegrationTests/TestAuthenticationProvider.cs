using System.Net.Mqtt;

namespace IntegrationTests
{
	public class TestAuthenticationProvider : IMqttAuthenticationProvider
	{
		readonly string expectedUsername;
		readonly string expectedPassword;

		public TestAuthenticationProvider (string expectedUsername, string expectedPassword)
		{
			this.expectedUsername = expectedUsername;
			this.expectedPassword = expectedPassword;
		}

		public bool Authenticate (string clientId, string username, string password)
		{
			return username == expectedUsername && password == expectedPassword;
		}
	}
}
