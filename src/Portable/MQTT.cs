namespace Hermes
{
	public class MQTT
	{
		public const string Name = "MQTT";

		public const int ProtocolLevel = 4;

		public const int PacketTypeLength = 1;

		public const int StringPrefixLength = 2;

		public const int ClientIdMaxLength = 23;

		public static readonly int NameLength = MQTT.Name.Length + MQTT.StringPrefixLength;
	}
}
