using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Hermes.Packets;

namespace Hermes
{
	public class ConnectionProvider : IConnectionProvider
	{
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
				this.RemoveConnection (clientId);
			}

			connections.TryAdd(clientId, connection);
        }

		public IChannel<IPacket> GetConnection (string clientId)
		{
			var existingConnection = default(IChannel<IPacket>);

			if (connections.TryGetValue (clientId, out existingConnection)) {
				if (!existingConnection.IsConnected) {
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
				existingConnection.Dispose ();
			}
        }
	}
}
