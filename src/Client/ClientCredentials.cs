namespace System.Net.Mqtt
{
	public class ClientCredentials
	{
		public ClientCredentials (string clientId)
		{
			this.ClientId = clientId;
		}

		public ClientCredentials (string clientId, string userName, string password)
		{
			this.ClientId = clientId;
			this.UserName = userName;
			this.Password = password;
		}

		public string ClientId { get; private set; }

		public string UserName { get; private set; }

		public string Password { get; private set; }
	}
}
