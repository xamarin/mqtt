namespace System.Net.Mqtt.Packets
{
    /// <summary>
    /// Represents the status of the MQTT connection
    /// </summary>
    /// <remarks>
    /// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/errata01/os/mqtt-v3.1.1-errata01-os-complete.html#_Toc442180843">Variable Header</a>
    /// section for more details on the connection status values
    /// </remarks>
	public enum MqttConnectionStatus : byte
    {
        Accepted = 0x00,
        UnacceptableProtocolVersion = 0x01,
        IdentifierRejected = 0x02,
        ServerUnavailable = 0x03,
        BadUserNameOrPassword = 0x04,
        NotAuthorized = 0x05,
    }
}
