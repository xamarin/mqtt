using Hermes.Packets;

namespace Hermes
{
	public class ProtocolConfiguration : IProtocolConfiguration
	{
		public int Port { get; set; }

		public QualityOfService MaximumQualityOfService { get; set; }

		public int ConnectTimeWindow { get; set; }

		public ushort KeepAlive { get; set; }

		public int AckTimeout { get; set; }
	}
}
