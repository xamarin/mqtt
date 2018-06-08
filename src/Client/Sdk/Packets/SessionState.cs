namespace System.Net.Mqtt
{
	/// <summary>
	/// Represents the state of the client session created as part of the MQTT connection
	/// </summary>
	/// <remarks>
	/// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html#_Toc385349255">Session Present</a>
	/// section for more details on the session state handling
	/// </remarks>
	public enum SessionState
	{
		/// <summary>
		/// The client session is new. In order to receive messages for certain topics, subscriptions need to be set up.
		/// </summary>
		CleanSession,
		/// <summary>
		/// The client session has been re used, including any existing subscriptions.
		/// </summary>
		SessionPresent
	}
}
