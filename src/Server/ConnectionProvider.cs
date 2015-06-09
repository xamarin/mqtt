using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Hermes.Diagnostics;
using Hermes.Packets;
using Hermes.Properties;

namespace Hermes
{
	public class ConnectionProvider : IConnectionProvider
	{
		static readonly ITracer tracer = Tracer.Get<ConnectionProvider> ();
		static readonly ConcurrentDictionary<string, IChannel<IPacket>> connections;

		static ConnectionProvider ()
		{
			connections = new ConcurrentDictionary<string, IChannel<IPacket>> ();
		}

		public int Connections { get { return connections.Skip(0).Count(); } }

		public IEnumerable<string> ActiveClients 
		{ 
			get 
			{ 
				return connections
					.Where (c => c.Value.IsConnected)
					.Select (c => c.Key); 
			} 
		}

		public void AddConnection(string clientId, IChannel<IPacket> connection)
        {
			var existingConnection = default (IChannel<IPacket>);

			if (connections.TryGetValue (clientId, out existingConnection)) {
				tracer.Warn (Resources.Tracer_ConnectionProvider_ClientIdExists, clientId);

				this.RemoveConnection (clientId);
			}

			connections.TryAdd(clientId, connection);
        }

		public IChannel<IPacket> GetConnection (string clientId)
		{
			var existingConnection = default(IChannel<IPacket>);

			if (connections.TryGetValue (clientId, out existingConnection)) {
				if (!existingConnection.IsConnected) {
					tracer.Warn (Resources.Tracer_ConnectionProvider_ClientDisconnected, clientId);

					this.RemoveConnection (clientId);
					existingConnection = default (IChannel<IPacket>);
				}
			}

			return existingConnection;
		}

		/// <exception cref="ProtocolException">ProtocolException</exception>
        public void RemoveConnection(string clientId)
        {
			var existingConnection = default (IChannel<IPacket>);

			if (connections.TryRemove (clientId, out existingConnection)) {
				tracer.Info (Resources.Tracer_ConnectionProvider_RemovingClient, clientId);

				existingConnection.Dispose ();
			}
        }
	}
}
