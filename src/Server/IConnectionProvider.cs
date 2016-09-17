using System.Collections.Generic;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt
{
    internal interface IConnectionProvider
	{
		int Connections { get; }

		IEnumerable<string> ActiveClients { get; }

        IEnumerable<string> PrivateClients { get; }

        void RegisterPrivateClient (string clientId);

		void AddConnection (string clientId, IMqttChannel<IPacket> connection);

		IMqttChannel<IPacket> GetConnection (string clientId);

		void RemoveConnection (string clientId);
	}
}
