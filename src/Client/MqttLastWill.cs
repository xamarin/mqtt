using System.ComponentModel;
using System.Text;
using System.Linq;

namespace System.Net.Mqtt
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
		[Obsolete ("This constructor is obsolete, please use the constructor with the byte[] payload parameter instead.")]
		[EditorBrowsable (EditorBrowsableState.Never)]
		public MqttLastWill (string topic, MqttQualityOfService qualityOfService, bool retain, string message)
			: this (topic, qualityOfService, retain, payload: Encoding.UTF8.GetBytes (message))
		{
			Message = message;
		}

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
		/// <param name="payload">Payload of the will message to publish</param>
		public MqttLastWill (string topic, MqttQualityOfService qualityOfService, bool retain, byte[] payload)
		{
			Topic = topic;
			QualityOfService = qualityOfService;
			Retain = retain;
			Payload = payload;
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
		[Obsolete ("This property is obsolete, please use the byte[] Payload property instead.")]
		[EditorBrowsable (EditorBrowsableState.Never)]
		public string Message { get; }

		/// <summary>
		/// Payload of the will message
		/// </summary>
		public byte[] Payload { get; }

		/// <summary>
		/// Determines whether this instance and another 
		/// specified <see cref="MqttLastWill" /> have
		/// the same values
		/// </summary>
		/// <param name="other">The <see cref="MqttLastWill" /> to compare to this instance</param>
		/// <returns>true if the values of the <paramref name="other"/> parameter 
		/// are the same values of this instance, otherwise returns false.
		/// If <paramref name="other"/> is null, the method returns false
		///</returns>
		public bool Equals (MqttLastWill other)
		{
			if (other == null)
				return false;

			return Topic == other.Topic &&
				QualityOfService == other.QualityOfService &&
				Retain == other.Retain &&
				Payload.SequenceEqual (other.Payload);
		}

        /// <summary>
        /// Determines whether this instance and a specified object, 
        /// which must also be a <see cref="MqttLastWill" />,
        /// have the same values
        /// </summary>
        /// <param name="obj">The <see cref="MqttLastWill" /> to compare to this instance</param>
        /// <returns>true if the values of the <paramref name="obj"/> parameter 
        /// are the same values of this instance, otherwise returns false.
        /// If <paramref name="obj"/> is null, the method returns false
        ///</returns>
		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var will = obj as MqttLastWill;

			if (will == null)
				return false;

			return Equals (will);
		}

        /// <summary>
        /// Determines whether the specified instances of 
        /// <see cref="MqttLastWill" /> have the same values
        /// </summary>
        /// <param name="will">The first <see cref="MqttLastWill" /> to compare</param>
        /// <param name="other">The second <see cref="MqttLastWill" /> to compare</param>
        /// <returns>true if the values of the <paramref name="will"/> parameter 
        /// and the <paramref name="other"/> parameter are the same, otherwise returns false.
        /// If <paramref name="will"/> or <paramref name="other"/> is null, the method returns false
        ///</returns>
		public static bool operator == (MqttLastWill will, MqttLastWill other)
		{
			if ((object)will == null || (object)other == null)
				return Object.Equals (will, other);

			return will.Equals (other);
		}

        /// <summary>
        /// Determines whether the specified instances of 
        /// <see cref="MqttLastWill" /> have different values
        /// </summary>
        /// <param name="will">The first <see cref="MqttLastWill" /> to compare</param>
        /// <param name="other">The second <see cref="MqttLastWill" /> to compare</param>
        /// <returns>true if the values of the <paramref name="will"/> parameter 
        /// and the <paramref name="other"/> parameter are different, otherwise returns false.
        /// If <paramref name="will"/> or <paramref name="other"/> is null, the method returns false
        ///</returns>
		public static bool operator != (MqttLastWill will, MqttLastWill other)
		{
			if ((object)will == null || (object)other == null)
				return !Object.Equals (will, other);

			return !will.Equals (other);
		}

        /// <summary>
        /// Returns the hash code for this <see cref="MqttLastWill" /> instance 
        /// </summary>
        /// <returns>A 32-bit signed integer hash code</returns>
		public override int GetHashCode ()
		{
			return Topic.GetHashCode () + Payload.GetHashCode ();
		}
	}
}
