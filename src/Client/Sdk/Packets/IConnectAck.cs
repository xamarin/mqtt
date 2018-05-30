namespace System.Net.Mqtt
{
	/// <summary>
	/// Represents the acknowledgement of an MQTT connection
	/// </summary>
	public interface IConnectAck
    {
		/// <summary>
		/// Value that indicates if the session has been re used, as consequence of the cleanSession parameter used in the connection
		/// </summary>
		bool SessionPresent { get; }
	}
}
