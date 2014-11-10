using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes
{
	public class ClientManager : IClientManager
	{
		//TODO: We should make this a ConcurrentDicionary (check about PCL compatibility)
        static readonly IDictionary<string, IChannel<IPacket>> clientConnections;

		static ClientManager()
		{
			clientConnections = new Dictionary<string, IChannel<IPacket>> ();
		}

        public void Add(string clientId, IChannel<IPacket> connection)
        {
			if (clientConnections.Any (c => c.Key == clientId))
				throw new ProtocolException ();

			clientConnections.Add (clientId, connection);
        }

        public async Task SendMessageAsync(string clientId, IPacket packet)
        {
			var connection = default (IChannel<IPacket>);

			if (!clientConnections.TryGetValue (clientId, out connection))
				throw new ProtocolException ();

			await connection.SendAsync (packet);
        }

        public void Remove(string clientId)
        {
            if (!clientConnections.Any (c => c.Key == clientId))
				throw new ProtocolException ();

			clientConnections.Remove (clientId);
        }
	}
}
