using System.Runtime.Serialization;

namespace System.Net.Mqtt.Client
{
	[DataContract]
	public class ClientException : Exception
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
	}
}
