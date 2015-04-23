using System;
using System.Runtime.Serialization;

namespace Hermes
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
