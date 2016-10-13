﻿using System.Diagnostics;
using System.Net.Mqtt.Sdk.Packets;
using System.Net.Mqtt.Sdk.Storage;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Sdk.Flows
{
	internal class DisconnectFlow : IProtocolFlow
	{
		static readonly ITracer tracer = Tracer.Get<DisconnectFlow> ();

		readonly IConnectionProvider connectionProvider;
		readonly IRepository<ClientSession> sessionRepository;
		readonly IRepository<ConnectionWill> willRepository;

		public DisconnectFlow (IConnectionProvider connectionProvider,
			IRepository<ClientSession> sessionRepository,
			IRepository<ConnectionWill> willRepository)
		{
			this.connectionProvider = connectionProvider;
			this.sessionRepository = sessionRepository;
			this.willRepository = willRepository;
		}

		public async Task ExecuteAsync (string clientId, IPacket input, IMqttChannel<IPacket> channel)
		{
			if (input.Type != MqttPacketType.Disconnect) {
				return;
			}

			await Task.Run (() => {
				var disconnect = input as Disconnect;

				tracer.Info (Server.Properties.Resources.DisconnectFlow_Disconnecting, clientId);

				willRepository.Delete (w => w.ClientId == clientId);

				var session = sessionRepository.Get (s => s.ClientId == clientId);

				if (session == null) {
					throw new MqttException (string.Format (Properties.Resources.SessionRepository_ClientSessionNotFound, clientId));
				}

				if (session.Clean) {
					sessionRepository.Delete (session);

					tracer.Info (Server.Properties.Resources.Server_DeletedSessionOnDisconnect, clientId);
				}

				connectionProvider.RemoveConnection (clientId);
			});
		}
	}
}
