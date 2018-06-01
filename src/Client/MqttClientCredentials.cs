namespace System.Net.Mqtt
{
    /// <summary>
    /// Credentials used to connect a Client to a Server as part of the protocol connection
    /// </summary>
	public class MqttClientCredentials
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MqttClientCredentials" /> class 
		/// specifying the id of client to connect
		/// </summary>
		/// <param name="clientId">Id of the client to connect</param>
		public MqttClientCredentials (string clientId)
			: this (clientId, userName: string.Empty, password: string.Empty)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MqttClientCredentials" /> class 
		/// specifying the id of client to connect, the username and password
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

		internal MqttClientCredentials () : this (clientId: string.Empty)
		{
		}

		/// <summary>
		/// Id of the client to connect
		/// The Client Id must contain only the characters 0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ
		/// and have a maximum of 23 encoded bytes. 
		/// It can also be null or empty, in which case the Broker will generate and assign it
		/// </summary>
		public string ClientId { get; }

        /// <summary>
        /// User Name used for authentication
        /// Authentication is not mandatory on MQTT and is up to the consumer of the API
        /// </summary>
		public string UserName { get; }

        /// <summary>
        /// Password used for authentication
        /// Authentication is not mandatory on MQTT and is up to the consumer of the API
        /// </summary>
		public string Password { get; }
	}
}
