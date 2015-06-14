using System.Runtime.Serialization;

namespace System.Net.Mqtt
{
	[Serializable]
	public class ProtocolViolationException : ProtocolException
	{
		public ProtocolViolationException ()
		{
		}

		public ProtocolViolationException (string message) : base(message)
		{
		}

		public ProtocolViolationException (string message, Exception innerException) : base(message, innerException)
		{
		}

		protected ProtocolViolationException (SerializationInfo info, StreamingContext context)
			: base (info, context)
		{
		}
	}
}
