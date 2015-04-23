using System;
using System.Runtime.Serialization;
using Hermes.Packets;

namespace Hermes
{
	[Serializable]
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

		protected ProtocolConnectionException (SerializationInfo info, StreamingContext context)
			: base (info, context)
		{
		}

		public ConnectionStatus ReturnCode { get; set; }
	}
}
