using System.Collections.Generic;
using System.Net.Mqtt.Sdk.Packets;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Sdk
{
	internal interface IConnectionProvider
	{
		int Connections { get; }

		IEnumerable<string> ActiveClients { get; }

		IEnumerable<string> PrivateClients { get; }

		void RegisterPrivateClient(string clientId);

		Task AddConnectionAsync(string clientId, IMqttChannel<IPacket> connection);

		Task<IMqttChannel<IPacket>> GetConnectionAsync(string clientId);

		Task RemoveConnectionAsync(string clientId);
	}
}
