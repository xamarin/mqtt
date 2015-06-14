using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Storage
{
	public class ConnectionWill : StorageObject
	{
		public string ClientId { get; set; }

		public Will Will { get; set; }
	}
}
