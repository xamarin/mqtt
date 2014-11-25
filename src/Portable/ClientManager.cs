using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Properties;

namespace Hermes
{
	public class ClientManager : IClientManager
	{
		//TODO: We should control concurrency in this list (ConcurrentDictionary is not available on PCL's)
		static readonly IDictionary<string, IChannel<IPacket>> clientConnections = new Dictionary<string, IChannel<IPacket>> ();

		public IEnumerable<string> Clients { get { return clientConnections.Keys; } }

		public void AddClient(string clientId, IChannel<IPacket> connection)
        {
			var existingConnection = clientConnections.FirstOrDefault (c => c.Key == clientId);

			if (!existingConnection.Equals(default(KeyValuePair<string, IChannel<IPacket>>))) {
				this.RemoveClient (clientId);
				existingConnection.Value.Close ();
			}

			clientConnections.Add (clientId, connection);
        }

		/// <exception cref="ProtocolException">ProtocolException</exception>
        public async Task SendMessageAsync(string clientId, IPacket packet)
        {
			var connection = default (IChannel<IPacket>);

			if (!clientConnections.TryGetValue (clientId, out connection)) {
				var error = string.Format (Resources.ClientManager_ClientIdNotFound, clientId);
				
				throw new ProtocolException (error);
			}

			await connection.SendAsync (packet);
        }

        public void RemoveClient(string clientId)
        {
            if (!clientConnections.Any (c => c.Key == clientId)){
				var error = string.Format (Resources.ClientManager_ClientIdNotFound, clientId);
				
				throw new ProtocolException (error);
			}

			clientConnections.Remove (clientId);
        }
	}
}
