using System;

namespace Hermes
{
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
	}
}
