using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Server
{
	internal class ConnectionProvider : IConnectionProvider
	{
		static readonly ConcurrentDictionary<string, IChannel<IPacket>> connections;

		readonly ITracer tracer;

		static ConnectionProvider ()
		{
			connections = new ConcurrentDictionary<string, IChannel<IPacket>> ();
		}

		public ConnectionProvider (ITracerManager tracerManager)
		{
			tracer = tracerManager.Get<ConnectionProvider> ();
		}

		public int Connections { get { return connections.Skip (0).Count (); } }

		public IEnumerable<string> ActiveClients
		{
			get
			{
				return connections
					.Where (c => c.Value.IsConnected)
					.Select (c => c.Key);
			}
		}

		public void AddConnection (string clientId, IChannel<IPacket> connection)
		{
			var existingConnection = default (IChannel<IPacket>);

			if (connections.TryGetValue (clientId, out existingConnection)) {
				tracer.Warn (Properties.Resources.Tracer_ConnectionProvider_ClientIdExists, clientId);

				RemoveConnection (clientId);
			}

			connections.TryAdd (clientId, connection);
		}

		public IChannel<IPacket> GetConnection (string clientId)
		{
			var existingConnection = default(IChannel<IPacket>);

			if (connections.TryGetValue (clientId, out existingConnection)) {
				if (!existingConnection.IsConnected) {
					tracer.Warn (Properties.Resources.Tracer_ConnectionProvider_ClientDisconnected, clientId);

					RemoveConnection (clientId);
					existingConnection = default (IChannel<IPacket>);
				}
			}

			return existingConnection;
		}

		/// <exception cref="ProtocolException">ProtocolException</exception>
		public void RemoveConnection (string clientId)
		{
			var existingConnection = default (IChannel<IPacket>);

			if (connections.TryRemove (clientId, out existingConnection)) {
				tracer.Info (Properties.Resources.Tracer_ConnectionProvider_RemovingClient, clientId);

				existingConnection.Dispose ();
			}
		}
	}
}
