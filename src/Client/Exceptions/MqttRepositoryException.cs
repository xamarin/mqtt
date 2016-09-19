using System.Runtime.Serialization;

namespace System.Net.Mqtt.Exceptions
{
    /// <summary>
    /// The exception thrown when an operation with the data repository used to store MQTT information fails
    /// </summary>
	[DataContract]
	public class MqttRepositoryException : MqttException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MqttRepositoryException" /> class
        /// </summary>
        public MqttRepositoryException ()
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttRepositoryException" /> class,
        /// using the specified error message
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
		public MqttRepositoryException (string message) : base (message)
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttRepositoryException" /> class,
        /// with a specified error message and a reference to the inner exception that is the cause
        /// of this exception
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
		public MqttRepositoryException (string message, Exception innerException) : base (message, innerException)
		{
		}
	}
}
