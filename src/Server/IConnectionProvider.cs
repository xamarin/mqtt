﻿using System.Collections.Generic;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Server
{
	internal interface IConnectionProvider
	{
		int Connections { get; }

		IEnumerable<string> ActiveClients { get; }

		void AddConnection (string clientId, IMqttChannel<IPacket> connection);

		IMqttChannel<IPacket> GetConnection (string clientId);

		void RemoveConnection (string clientId);
	}
}
