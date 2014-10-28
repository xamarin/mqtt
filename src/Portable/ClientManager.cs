using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes
{
	public class ClientManager : IClientManager
	{
		//TODO: We should make this a ConcurrentDicionary (check about PCL compatibility)
        static readonly IDictionary<string, IProtocolConnection> userConnections;

		static ClientManager()
		{
			userConnections = new Dictionary<string, IProtocolConnection> ();
		}

        public void Add(IProtocolConnection connection)
        {
			if (connection.IsPending)
				throw new ProtocolException ();

			if (userConnections.Any (c => c.Key == connection.ClientId))
				throw new ProtocolException ();

			userConnections.Add (connection.ClientId, connection);
        }

        public async Task SendMessageAsync(string clientId, IPacket packet)
        {
			var connection = default (IProtocolConnection);

			if (!userConnections.TryGetValue (clientId, out connection))
				throw new ProtocolException ();

			await connection.SendAsync (packet);
        }

        public void Remove(string clientId)
        {
            if (!userConnections.Any (c => c.Key == clientId))
				throw new ProtocolException ();

			userConnections.Remove (clientId);
        }
	}
}
