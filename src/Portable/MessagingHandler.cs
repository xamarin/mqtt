using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hermes.Flows;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes
{
	public class MessagingHandler : IMessagingHandler, IClientManager
	{
		//TODO: We should make this a ConcurrentDicionary (check about PCL compatibility)
		static readonly IDictionary<string, IChannel<IPacket>> clientConnections;

		readonly IDictionary<ProtocolFlowType, IProtocolFlow> flows;

		static MessagingHandler()
		{
			clientConnections = new Dictionary<string, IChannel<IPacket>> ();
		}

		public MessagingHandler (IProtocolConfiguration configuration, IRepository<ClientSession> sessionRepository, 
			IRepository<RetainedMessage> retainedRepository, IRepository<ConnectionWill> willRepository, IRepository<PacketIdentifier> packetIdentifierRepository)
		{
			this.flows = new Dictionary<ProtocolFlowType, IProtocolFlow>();

			this.flows.Add (ProtocolFlowType.Connect, new ConnectFlow (sessionRepository, willRepository));
			this.flows.Add (ProtocolFlowType.Publish, new PublishFlow (configuration, this, retainedRepository, sessionRepository, packetIdentifierRepository));
			this.flows.Add (ProtocolFlowType.Subscribe, new SubscribeFlow (configuration, sessionRepository, packetIdentifierRepository));
			this.flows.Add (ProtocolFlowType.Unsubscribe, new UnsubscribeFlow (sessionRepository, packetIdentifierRepository));
			this.flows.Add (ProtocolFlowType.Ping, new PingFlow ());
			this.flows.Add (ProtocolFlowType.Disconnect, new DisconnectFlow (this, willRepository));
		}

		public void Handle (IChannel<IPacket> channel)
		{
			var clientId = string.Empty;

			channel.Receiver.Subscribe (async packet => {
				if (packet is Connect) {
					clientId = ((Connect)packet).ClientId;

					this.Add (clientId, channel);
				}

				var flowType = packet.Type.ToFlowType ();
				var flow = default (IProtocolFlow);

				if (!this.flows.TryGetValue (flowType, out flow)) {
					var error = string.Format (Resources.ProtocolFlowProvider_UnknownPacketType, packet.Type);
				
					throw new ProtocolException (error);
				}

				await flow.ExecuteAsync (clientId, packet, channel);
			});
		}

		public void Add(string clientId, IChannel<IPacket> connection)
        {
			var existingConnection = clientConnections.FirstOrDefault (c => c.Key == clientId);

			if (!existingConnection.Equals(default(KeyValuePair<string, IChannel<IPacket>>))) {
				this.Remove (clientId);
				existingConnection.Value.Close ();
			}

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
