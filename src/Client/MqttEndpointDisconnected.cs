namespace System.Net.Mqtt
{
    /// <summary>
    /// Reason of an MQTT Client disconnection
    /// </summary>
	public enum DisconnectedReason
	{
        /// <summary>
        /// Disconnected by the remote host
        /// </summary>
		RemoteDisconnected,
        /// <summary>
        /// Disconnected by the client itself, on purpose
        /// This could mean a protocol disconnect or a dispose of the <see cref="IMqttClient" /> 
        /// </summary>
		Disposed,
        /// <summary>
        /// Disconnected because of an unexpected error on the Client
        /// </summary>
		Error
	}

    /// <summary>
    /// Represents the disconnection information produced by the
    /// <see cref="IMqttClient.Disconnected" /> event 
    /// </summary>
	public class MqttEndpointDisconnected
	{
		public MqttEndpointDisconnected (DisconnectedReason reason, string message = null)
		{
			Reason = reason;
			Message = message;
		}

        /// <summary>
        /// Reason of the disconnection
        /// See <see cref="DisconnectedReason" /> for more details on the supported values 
        /// </summary>
		public DisconnectedReason Reason { get; private set; }

        /// <summary>
        /// Message that explains the disconnection cause
        /// </summary>
		public string Message { get; private set; }
	}
}
