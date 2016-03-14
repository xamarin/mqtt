namespace System.Net.Mqtt.Client
{
	public class ClientCredentials
	{
		public ClientCredentials (string clientId)
		{
			ClientId = clientId;
		}

		public ClientCredentials (string clientId, string userName, string password)
		{
			ClientId = clientId;
			UserName = userName;
			Password = password;
		}

		public string ClientId { get; private set; }

		public string UserName { get; private set; }

		public string Password { get; private set; }
	}
}
