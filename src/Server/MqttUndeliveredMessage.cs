namespace System.Net.Mqtt
{
    /// <summary>
    /// Represents a published application message that had no subscribers
    /// for the pusblished topic
    /// This information is exposed by the <see cref="IMqttServer.MessageUndelivered" /> event 
    /// </summary>
	public class MqttUndeliveredMessage
    {
        /// <summary>
        /// Client Id of the publisher of the message
        /// </summary>
		public string SenderId { get; set; }

        /// <summary>
        /// Application message that was not delivered
        /// See <see cref="MqttApplicationMessage" /> for more details about the
        /// format of the application message
        /// </summary>
		public MqttApplicationMessage Message { get; set; }
	}
}
