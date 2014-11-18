using Hermes.Flows;

namespace Hermes
{
	public class Protocol
	{
		public const string Name = "MQTT";

		public const int DefaultPort = 1883;

		public const int SupportedLevel = 4;

		public const int PacketTypeLength = 1;

		public const int StringPrefixLength = 2;

		public const int ClientIdMaxLength = 23;

		public static readonly int NameLength = Protocol.Name.Length + Protocol.StringPrefixLength;

		public static ProtocolEncoding Encoding { get; private set; }

		static Protocol()
		{
			Encoding = new ProtocolEncoding();
		}
	}
}
