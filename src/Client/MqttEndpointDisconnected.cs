namespace System.Net.Mqtt
{
    /// <summary>
    /// Reason of an MQTT Client or Server disconnection
    /// </summary>
	public enum DisconnectedReason
	{
        /// <summary>
        /// Disconnected by the remote host
        /// </summary>
        /// <remarks>
        /// This reason is used only on Client disconnections
        /// </remarks>
		RemoteDisconnected,
        /// <summary>
        /// Disconnected by the endpoint itself
        /// This could mean a protocol Disconnect in case of Clients,
        /// a Stop in case of Servers or an explicit Dispose 
        /// of the corresponding endpoint instance
        /// </summary>
		SelfDisconnected,
        /// <summary>
        /// Disconnected because of an unexpected error on the endpoint, 
        /// being this the Client or Server
        /// </summary>
		Error
    }

    /// <summary>
    /// Represents the disconnection information produced by 
    /// a disconnection event fired by a Client or Server instance
    /// </summary>
	public class MqttEndpointDisconnected
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="MqttEndpointDisconnected" /> class,
        /// specifying the disconnection reason and an optional disconnection message
        /// </summary>
        /// <param name="reason">
        /// Reason of the disconnection.
        /// See <see cref="DisconnectedReason" /> for more details about the possible options 
        /// </param>
        /// <param name="message">Optional message for the disconnection</param>
		public MqttEndpointDisconnected (DisconnectedReason reason, string message = null)
		{
			Reason = reason;
			Message = message;
		}

        /// <summary>
        /// Reason of the disconnection
        /// See <see cref="DisconnectedReason" /> for more details on the supported values 
        /// </summary>
		public DisconnectedReason Reason { get; }

        /// <summary>
        /// Message that explains the disconnection cause
        /// </summary>
		public string Message { get; }
	}
}
