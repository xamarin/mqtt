namespace System.Net.Mqtt
{
	public class MqttProtocol
	{
		public const string Name = "MQTT";

		public const int DefaultSecurePort = 8883;

		public const int DefaultNonSecurePort = 1883;

		public const int SupportedLevel = 4;

		public const string SingleLevelTopicWildcard = "+";

		public const string MultiLevelTopicWildcard = "#";

		public const int MaxIntegerLength = 65535;

		public const int StringPrefixLength = 2;

		public const int PacketTypeLength = 1;

		public const int ClientIdMaxLength = 23;

		public static readonly int NameLength = Name.Length + MqttProtocol.StringPrefixLength;

		public static MqttEncoder Encoding { get; private set; }

		static MqttProtocol ()
		{
			Encoding = new MqttEncoder ();
		}
	}
}
