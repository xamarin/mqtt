namespace System.Net.Mqtt
{
	public class MqttClientCredentials
	{
		public MqttClientCredentials (string clientId)
		{
			ClientId = clientId;
		}

		public MqttClientCredentials (string clientId, string userName, string password)
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
