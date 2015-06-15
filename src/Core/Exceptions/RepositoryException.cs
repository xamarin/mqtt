using System.Runtime.Serialization;

namespace System.Net.Mqtt.Exceptions
{
	[Serializable]
	public class RepositoryException : Exception
	{
		public RepositoryException ()
		{
		}

		public RepositoryException (string message) : base(message)
		{
		}

		public RepositoryException (string message, Exception innerException) : base(message, innerException)
		{
		}

		protected RepositoryException (SerializationInfo info, StreamingContext context)
			: base (info, context)
		{
		}
	}
}
