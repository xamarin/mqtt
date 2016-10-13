using System.Runtime.Serialization;

namespace System.Net.Mqtt.Sdk.Storage
{
    /// <summary>
    /// The exception thrown when an operation with the data repository used to store MQTT information fails.
    /// </summary>
	[DataContract]
	public class RepositoryException : MqttException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryException" /> class
        /// </summary>
        public RepositoryException ()
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryException" /> class,
        /// using the specified error message
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
		public RepositoryException (string message) : base (message)
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryException" /> class,
        /// with a specified error message and a reference to the inner exception that is the cause
        /// of this exception
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
		public RepositoryException (string message, Exception innerException) : base (message, innerException)
		{
		}
	}
}
