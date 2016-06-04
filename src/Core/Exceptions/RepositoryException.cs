using System.Runtime.Serialization;

namespace System.Net.Mqtt.Exceptions
{
	[DataContract]
	public class RepositoryException : Exception
	{
		public RepositoryException ()
		{
		}

		public RepositoryException (string message) : base (message)
		{
		}

		public RepositoryException (string message, Exception innerException) : base (message, innerException)
		{
		}
	}
}
