namespace System.Net.Mqtt
{
    /// <summary>
    /// Represents the possible values accepted for the MQTT Quality of Service (QoS)
    /// The QoS is used on protocol publish to determine how Client and Server should acknowledge
    /// the published packets
    /// </summary>
    /// <remarks>
    /// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/mqtt-v3.1.1.html#_Toc442180912">Quality of Service levels and protocol flows</a>
    /// for more details about the QoS values and description
    /// </remarks>
	public enum MqttQualityOfService : byte
    {
        /// <summary>
        /// Represents the QoS level 0 (at most once delivery)
        /// </summary>
        AtMostOnce = 0x00,
        /// <summary>
        /// Represents the QoS level 1 (at least once delivery)
        /// </summary>
        AtLeastOnce = 0x01,
        /// <summary>
        /// Represents the QoS level 2 (exactly once delivery)
        /// </summary>
        ExactlyOnce = 0x02,
    }
}
