using System.Runtime.Serialization;

namespace System.Net.Mqtt.Server
{
	[DataContract]
	public class ServerException : Exception
	{
		public ServerException ()
		{
		}

		public ServerException (string message)
			: base (message)
		{
		}

		public ServerException (string message, Exception innerException)
			: base (message, innerException)
		{
		}
	}
}
