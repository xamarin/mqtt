using System;

namespace Hermes.Messages
{
	public class ConnectAck : Message
    {
        public ConnectAck(ConnectionStatus status, bool existingSession)
            : base(MessageType.ConnectAck)
        {
            this.Status = status;
			this.ExistingSession = existingSession;
        }

        public ConnectionStatus Status { get; private set; }

		public bool ExistingSession { get; private set; }
    }
}
