namespace System.Net.Mqtt
{
    /// <summary>
    /// Represents an application message, which correspond to the unit of information
    /// sent from Client to Broker and from Broker to Client
    /// </summary>
	public partial class MqttApplicationMessage
	{
		public MqttApplicationMessage ()
		{

		}

		public MqttApplicationMessage (string topic, byte[] payload)
		{
			Topic = topic;
			Payload = payload;
		}

        /// <summary>
        /// Topic associated with the message
        /// Any subscriber of this topic should receive the corresponding messages
        /// </summary>
		public string Topic { get; set; }

        /// <summary>
        /// Payload associated with the message
        /// </summary>
		public byte[] Payload { get; set; }
	}
}
