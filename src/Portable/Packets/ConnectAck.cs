using System;

namespace Hermes.Packets
{
	public class ConnectAck : IPacket, IEquatable<ConnectAck>
    {
        public ConnectAck(ConnectionStatus status, bool existingSession)
        {
            this.Status = status;
			this.ExistingSession = existingSession;
        }

		public PacketType Type { get { return PacketType.ConnectAck; }}

        public ConnectionStatus Status { get; private set; }

		public bool ExistingSession { get; private set; }

		public bool Equals (ConnectAck other)
		{
			if (other == null)
				return false;

			return this.Status == other.Status &&
				this.ExistingSession == other.ExistingSession;
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var connectAck = obj as ConnectAck;

			if (connectAck == null)
				return false;

			return this.Equals (connectAck);
		}

		public static bool operator == (ConnectAck connectAck, ConnectAck other)
		{
			if ((object)connectAck == null || (object)other == null)
				return Object.Equals(connectAck, other);

			return connectAck.Equals(other);
		}

		public static bool operator != (ConnectAck connectAck, ConnectAck other)
		{
			if ((object)connectAck == null || (object)other == null)
				return !Object.Equals(connectAck, other);

			return !connectAck.Equals(other);
		}

		public override int GetHashCode ()
		{
			return this.Status.GetHashCode () + this.ExistingSession.GetHashCode ();
		}
	}
}
