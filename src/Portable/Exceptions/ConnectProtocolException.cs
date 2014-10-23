using System;
using Hermes.Messages;

namespace Hermes
{
	public class ConnectProtocolException : ProtocolException
	{
		public ConnectProtocolException (ConnectionStatus status)
		{
			this.ReturnCode = status;
		}

		public ConnectProtocolException (ConnectionStatus status, string message) : base(message)
		{
			this.ReturnCode = status;
		}

		public ConnectProtocolException (ConnectionStatus status, string message, Exception innerException) : base(message, innerException)
		{
			this.ReturnCode = status;
		}

		public ConnectionStatus ReturnCode { get; set; }
	}
}
