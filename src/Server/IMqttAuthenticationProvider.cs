namespace System.Net.Mqtt
{
    /// <summary>
    /// Provides an extensibility point to implement authentication on MQTT
    /// Authentication mechanism is up to the consumer of the API as
    /// this library doesn't provide any pre-defined authentication mechanism.
    /// </summary>
	public interface IMqttAuthenticationProvider
	{
        /// <summary>
        /// Authenticates the user through the provided credentials
        /// </summary>
        /// <param name="clientId">Id of the client to authenticate</param>
        /// <param name="username">Username to authenticate</param>
        /// <param name="password">Password to authenticate</param>
        /// <returns>A boolean value indicating if the user has been authenticated or not</returns>
		bool Authenticate (string clientId, string username, string password);
	}
}
