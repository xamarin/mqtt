using System;
using Hermes.Messages;

namespace Hermes
{
	public class ProtocolConnectException : ProtocolException
	{
		public ProtocolConnectException (ConnectionStatus status)
		{
			this.ReturnCode = status;
		}

		public ProtocolConnectException (ConnectionStatus status, string message) : base(message)
		{
			this.ReturnCode = status;
		}

		public ProtocolConnectException (ConnectionStatus status, string message, Exception innerException) : base(message, innerException)
		{
			this.ReturnCode = status;
		}

		public ConnectionStatus ReturnCode { get; set; }
	}
}
