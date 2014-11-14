using Hermes.Packets;

namespace Hermes
{
	public interface IProtocolConfiguration
	{
		int Port { get; }

		QualityOfService MaximumQualityOfService { get; }

		int ConnectTimeWindow { get; }

		ushort KeepAlive { get; }

		int AckTimeout { get; }
	}
}
