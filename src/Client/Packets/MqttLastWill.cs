namespace System.Net.Mqtt.Packets
{
    /// <summary>
    /// Represents the last will message sent by the Server when a Client
    /// gets disconnected unexpectedely
    /// Any disconnection except the protocol disconnection is considered unexpected
    /// </summary>
	public class MqttLastWill : IEquatable<MqttLastWill>
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="MqttLastWill" /> class,
        /// specifying the topic to pusblish the last will message to, the Quality of Service (QoS)
        /// to use, if the message should be sent as a retained message and also the content of the will message
        /// to publish
        /// </summary>
        /// <param name="topic">Topic to publish the last will message to</param>
        /// <param name="qualityOfService">
        /// Quality of Service (QoS) to use when publishing the last will message.
        /// See <see cref="MqttQualityOfService" /> for more details about the QoS meanings
        /// </param>
        /// <param name="retain">Specifies if the message should be retained or not</param>
        /// <param name="message">Content of the will message to publish</param>
		public MqttLastWill (string topic, MqttQualityOfService qualityOfService, bool retain, string message)
		{
			Topic = topic;
			QualityOfService = qualityOfService;
			Retain = retain;
			Message = message;
		}

        /// <summary>
        /// Topic where the message will be published
        /// The Clients needs to subscribe to this topic in order to receive the will messages
        /// </summary>
		public string Topic { get; }

        /// <summary>
        /// Quality of Servive (QoS) associated to the will message,
        /// that will be used when the Server publishes it
        /// See <see cref="MqttQualityOfService" /> for more details about the QoS values
        /// </summary>
		public MqttQualityOfService QualityOfService { get; }

        /// <summary>
        /// Determines if the message is sent as a retained message or not
        /// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/mqtt-v3.1.1.html#_Toc442180851">fixed header</a>
        /// section for more information about retained messages
        /// </summary>
		public bool Retain { get; }

        /// <summary>
        /// Content of the will message
        /// </summary>
		public string Message { get; }

		public bool Equals (MqttLastWill other)
		{
			if (other == null)
				return false;

			return Topic == other.Topic &&
				QualityOfService == other.QualityOfService &&
				Retain == other.Retain &&
				Message == other.Message;
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var will = obj as MqttLastWill;

			if (will == null)
				return false;

			return Equals (will);
		}

		public static bool operator == (MqttLastWill will, MqttLastWill other)
		{
			if ((object)will == null || (object)other == null)
				return Object.Equals (will, other);

			return will.Equals (other);
		}

		public static bool operator != (MqttLastWill will, MqttLastWill other)
		{
			if ((object)will == null || (object)other == null)
				return !Object.Equals (will, other);

			return !will.Equals (other);
		}

		public override int GetHashCode ()
		{
			return Topic.GetHashCode () + Message.GetHashCode ();
		}
	}
}
