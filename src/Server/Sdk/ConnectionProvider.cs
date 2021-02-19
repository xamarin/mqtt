using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mqtt.Sdk.Packets;
using System.Threading.Tasks;
using ServerProperties = System.Net.Mqtt.Server.Properties;

namespace System.Net.Mqtt.Sdk
{
	internal class ConnectionProvider : IConnectionProvider
	{
        static readonly ITracer tracer = Tracer.Get<ConnectionProvider> ();
        static readonly IList<string> privateClients;
        static readonly ConcurrentDictionary<string, IMqttChannel<IPacket>> connections;
        static readonly object lockObject = new object();

		static ConnectionProvider ()
		{
            privateClients = new List<string>();
            connections = new ConcurrentDictionary<string, IMqttChannel<IPacket>> ();
		}

		public int Connections => connections.Count;

		public IEnumerable<string> ActiveClients
		{
			get
			{
				return connections
					.Where (c => c.Value.IsConnected)
					.Select (c => c.Key);
			}
		}

        public IEnumerable<string> PrivateClients => privateClients;

        public void RegisterPrivateClient (string clientId)
        {
            if (privateClients.Contains (clientId)) {
                var message = string.Format (ServerProperties.Resources.ConnectionProvider_PrivateClientAlreadyRegistered, clientId);

                throw new MqttServerException (message);
            }

            lock (lockObject) {
                privateClients.Add(clientId);
            }
        }

        public async Task AddConnectionAsync (string clientId, IMqttChannel<IPacket> connection)
		{
			if (connections.TryGetValue (clientId, out var existingConnection)) {
				tracer.Warn (ServerProperties.Resources.ConnectionProvider_ClientIdExists, clientId);

				await RemoveConnectionAsync (clientId).ConfigureAwait(continueOnCapturedContext: false);
			}

			connections.TryAdd (clientId, connection);
		}

		public async Task<IMqttChannel<IPacket>> GetConnectionAsync (string clientId)
		{
			if (connections.TryGetValue (clientId, out var existingConnection)) {
				if (!existingConnection.IsConnected) {
					tracer.Warn (ServerProperties.Resources.ConnectionProvider_ClientDisconnected, clientId);

					await RemoveConnectionAsync (clientId).ConfigureAwait(continueOnCapturedContext: false);
					existingConnection = default (IMqttChannel<IPacket>);
				}
			}

			return existingConnection;
		}

		public async Task RemoveConnectionAsync(string clientId)
		{
			if (connections.TryRemove (clientId, out var existingConnection)) {
				tracer.Info (ServerProperties.Resources.ConnectionProvider_RemovingClient, clientId);

				await existingConnection
					.CloseAsync ()
					.ConfigureAwait(continueOnCapturedContext: false);
			}

            if (privateClients.Contains (clientId))  {
                lock (lockObject) {
                    if (privateClients.Contains (clientId)) {
                        privateClients.Remove (clientId);
                    }
                }
            }
		}
    }
}
