using Hermes.Packets;

namespace Hermes
{
	public interface IProtocolConfiguration
	{
		int Port { get; }

		QualityOfService SupportedQualityOfService { get; }

		int ConnectTimeWindow { get; }
	}
}
