namespace System.Net.Mqtt
{
    /// <summary>
    /// Credentials used to connect a Client to a Broker as part of the protocol connection
    /// </summary>
	public class MqttClientCredentials
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="MqttClientCredentials" /> class 
        /// specifying the id of client to connect
        /// </summary>
        /// <param name="clientId">Id of the client to connect</param>
		public MqttClientCredentials (string clientId)
		{
			ClientId = clientId;
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttClientCredentials" /> class 
        /// specifying the id of client to connect, and the username and password
        /// for authentication
        /// </summary>
        /// <param name="clientId">Id of the client to connect</param>
        /// <param name="userName">Username for authentication</param>
        /// /// <param name="password">Password for authentication</param>
        public MqttClientCredentials (string clientId, string userName, string password)
		{
			ClientId = clientId;
			UserName = userName;
			Password = password;
        }

        /// <summary>
        /// Id of the client to connect
        /// The Client Id must contain only the characters 0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ
        /// and have a maximum of 23 encoded bytes
        /// </summary>
		public string ClientId { get; private set; }

        /// <summary>
        /// User Name used for authentication
        /// Authentication is not mandatory on MQTT and is up to the consumer of the API
        /// </summary>
		public string UserName { get; private set; }

        /// <summary>
        /// Password used for authentication
        /// Authentication is not mandatory on MQTT and is up to the consumer of the API
		public string Password { get; private set; }
	}
}
