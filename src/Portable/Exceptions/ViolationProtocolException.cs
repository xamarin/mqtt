using System;

namespace Hermes
{
	public class ViolationProtocolException : ProtocolException
	{
		public ViolationProtocolException ()
		{
		}

		public ViolationProtocolException (string message) : base(message)
		{
		}

		public ViolationProtocolException (string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
