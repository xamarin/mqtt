namespace System.Net.Mqtt
{
    /// <summary>
    /// Represents an application message, which correspond to the unit of information
    /// sent from Client to Server and from Server to Client
    /// </summary>
	public class MqttApplicationMessage
	{
		public MqttApplicationMessage (string topic, byte[] payload)
		{
			Topic = topic;
            Payload = payload;
		}

        /// <summary>
        /// Topic associated with the message
        /// Any subscriber of this topic should receive the corresponding messages
        /// </summary>
		public string Topic { get; }

        /// <summary>
        /// Payload associated with the message
        /// </summary>
		public byte[] Payload { get; }
	}
}
