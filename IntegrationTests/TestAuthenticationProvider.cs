using System.Net.Mqtt.Server;

namespace IntegrationTests
{
	public class TestAuthenticationProvider : IAuthenticationProvider
	{
		readonly string expectedUsername;
		readonly string expectedPassword;

		public TestAuthenticationProvider (string expectedUsername, string expectedPassword)
		{
			this.expectedUsername = expectedUsername;
			this.expectedPassword = expectedPassword;
		}

		public bool Authenticate (string username, string password)
		{
			return username == this.expectedUsername && password == this.expectedPassword;
		}
	}
}
