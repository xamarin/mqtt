namespace System.Net.Mqtt
{
    /// <summary>
    /// Represents the status of the MQTT connection
    /// </summary>
    /// <remarks>
    /// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/mqtt-v3.1.1.html#_Toc442180843">Variable Header</a>
    /// section for more details on the connection status values
    /// </remarks>
	public enum MqttConnectionStatus : byte
    {
        /// <summary>
        /// Connection accepted
        /// </summary>
        Accepted = 0x00,
        /// <summary>
        /// The Server does not support the level 
        /// of the MQTT protocol requested by the Client
        /// </summary>
        UnacceptableProtocolVersion = 0x01,
        /// <summary>
        /// The Client identifier is correct 
        /// UTF-8 but not allowed by the Server
        /// </summary>
        IdentifierRejected = 0x02,
        /// <summary>
        /// The Network Connection has been made 
        /// but the MQTT service is unavailable
        /// </summary>
        ServerUnavailable = 0x03,
        /// <summary>
        /// The data in the user name or password is malformed
        /// </summary>
        BadUserNameOrPassword = 0x04,
        /// <summary>
        /// The Client is not authorized to connect
        /// </summary>
        NotAuthorized = 0x05,
    }
}
