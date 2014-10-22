using System;
using Hermes.Messages;

namespace Hermes
{
	public class ProtocolConnectionException : ProtocolException
	{
		public ProtocolConnectionException (ConnectionStatus status)
		{
			this.ReturnCode = status;
		}

		public ProtocolConnectionException (ConnectionStatus status, string message) : base(message)
		{
			this.ReturnCode = status;
		}

		public ProtocolConnectionException (ConnectionStatus status, string message, Exception innerException) : base(message, innerException)
		{
			this.ReturnCode = status;
		}

		public ConnectionStatus ReturnCode { get; set; }
	}
}
