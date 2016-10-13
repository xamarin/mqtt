using System.Runtime.Serialization;

namespace System.Net.Mqtt
{
    /// <summary>
    /// The exception that is thrown when a server operation fails
    /// </summary>
    [DataContract]
	public class MqttServerException : MqttException
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="MqttServerException" /> class
        /// </summary>
		public MqttServerException ()
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttServerException" /> class,
        /// using the specified error message
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        public MqttServerException (string message)
			: base (message)
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttServerException" /> class,
        /// with a specified error message and a reference to the inner exception that is the cause
        /// of this exception
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public MqttServerException (string message, Exception innerException)
			: base (message, innerException)
		{
		}
	}
}
