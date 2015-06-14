using System.Runtime.Serialization;

namespace System.Net.Mqtt
{
	[Serializable]
	public class ClientException : ApplicationException
	{
		public ClientException ()
		{
		}

		public ClientException (string message)
			: base (message)
		{
		}

		public ClientException (string message, Exception innerException)
			: base (message, innerException)
		{
		}

		protected ClientException (SerializationInfo info, StreamingContext context)
			: base (info, context)
		{
		}
	}
}
