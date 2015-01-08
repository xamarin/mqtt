using System.Collections.Concurrent;
using Hermes.Packets;
using Hermes.Properties;

namespace Hermes
{
	public class ConnectionProvider : IConnectionProvider
	{
		readonly ConcurrentDictionary<string, IChannel<IPacket>> connections;

		public ConnectionProvider ()
		{
			this.connections = new ConcurrentDictionary<string, IChannel<IPacket>> ();
		}

		public int Connections { get { return this.connections.Count; } }

		public void AddConnection(string clientId, IChannel<IPacket> connection)
        {
			var existingConnection = default (IChannel<IPacket>);

			if (this.connections.TryGetValue (clientId, out existingConnection)) {
				this.RemoveConnection (clientId);
				existingConnection.Dispose ();
			}

			this.connections.TryAdd(clientId, connection);
        }

		/// <exception cref="ProtocolException">ProtocolException</exception>
		public IChannel<IPacket> GetConnection (string clientId)
		{
			var existingConnection = default (IChannel<IPacket>);

			if (!this.connections.TryGetValue (clientId, out existingConnection)) {
				var error = string.Format (Resources.ConnectionProvider_ClientIdNotFound, clientId);
				
				throw new ProtocolException (error);
			}

			return existingConnection;
		}

		/// <exception cref="ProtocolException">ProtocolException</exception>
        public void RemoveConnection(string clientId)
        {
			var existingConnection = default (IChannel<IPacket>);

			if (!this.connections.TryGetValue (clientId, out existingConnection)) {
				var error = string.Format (Resources.ConnectionProvider_ClientIdNotFound, clientId);
				
				throw new ProtocolException (error);
			}

			this.connections.TryRemove (clientId, out existingConnection);
        }
	}
}
