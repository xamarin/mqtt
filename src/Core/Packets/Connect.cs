namespace System.Net.Mqtt.Packets
{
	public class Connect : IPacket, IEquatable<Connect>
	{
		public Connect (string clientId, bool cleanSession)
		{
			if (string.IsNullOrEmpty (clientId)) {
				throw new ArgumentNullException ("clientId");
			}

			this.ClientId = clientId;
			this.CleanSession = cleanSession;
			this.KeepAlive = 0;
		}

		public Connect ()
		{
			this.CleanSession = true;
			this.KeepAlive = 0;
		}

		public PacketType Type { get { return PacketType.Connect; }}

		public string ClientId { get; set; }

		public bool CleanSession { get; set; }

		public ushort KeepAlive { get; set; }

		public Will Will { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

		public bool Equals (Connect other)
		{
			if (other == null)
				return false;

			return this.ClientId == other.ClientId &&
				this.CleanSession == other.CleanSession &&
				this.KeepAlive == other.KeepAlive &&
				this.Will == other.Will &&
				this.UserName == other.UserName &&
				this.Password == other.Password;
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var connect = obj as Connect;

			if (connect == null)
				return false;

			return this.Equals (connect);
		}

		public static bool operator == (Connect connect, Connect other)
		{
			if ((object)connect == null || (object)other == null)
				return Object.Equals(connect, other);

			return connect.Equals(other);
		}

		public static bool operator != (Connect connect, Connect other)
		{
			if ((object)connect == null || (object)other == null)
				return !Object.Equals(connect, other);

			return !connect.Equals(other);
		}

		public override int GetHashCode ()
		{
			return this.ClientId.GetHashCode ();
		}
	}
}
