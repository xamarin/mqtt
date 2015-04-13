using Hermes.Packets;

namespace Hermes.Storage
{
	public class ConnectionWill : StorageObject
	{
		public string ClientId { get; set; }

		public Will Will { get; set; }
	}
}
