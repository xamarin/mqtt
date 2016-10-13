namespace System.Net.Mqtt.Sdk.Packets
{
	internal class Connect : IPacket, IEquatable<Connect>
	{
		public Connect (string clientId, bool cleanSession)
		{
			if (string.IsNullOrEmpty (clientId)) {
				throw new ArgumentNullException ("clientId");
			}

			ClientId = clientId;
			CleanSession = cleanSession;
			KeepAlive = 0;
		}

		public Connect ()
		{
			CleanSession = true;
			KeepAlive = 0;
		}

		public MqttPacketType Type { get { return MqttPacketType.Connect; } }

		public string ClientId { get; set; }

		public bool CleanSession { get; set; }

		public ushort KeepAlive { get; set; }

		public MqttLastWill Will { get; set; }

		public string UserName { get; set; }

		public string Password { get; set; }

		public bool Equals (Connect other)
		{
			if (other == null)
				return false;

			return ClientId == other.ClientId &&
				CleanSession == other.CleanSession &&
				KeepAlive == other.KeepAlive &&
				Will == other.Will &&
				UserName == other.UserName &&
				Password == other.Password;
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var connect = obj as Connect;

			if (connect == null)
				return false;

			return Equals (connect);
		}

		public static bool operator == (Connect connect, Connect other)
		{
			if ((object)connect == null || (object)other == null)
				return Object.Equals (connect, other);

			return connect.Equals (other);
		}

		public static bool operator != (Connect connect, Connect other)
		{
			if ((object)connect == null || (object)other == null)
				return !Object.Equals (connect, other);

			return !connect.Equals (other);
		}

		public override int GetHashCode ()
		{
			return ClientId.GetHashCode ();
		}
	}
}
