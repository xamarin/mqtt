using System;
using System.Collections.Generic;
using Hermes.Flows;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes
{
	public class MessagingHandler : IMessagingHandler
	{
		readonly IClientManager clientManager;
		readonly IDictionary<ProtocolFlowType, IProtocolFlow> flows;

		public MessagingHandler (IProtocolConfiguration configuration, IClientManager clientManager, IRepository<ProtocolSession> sessionRepository, 
			IRepository<RetainedMessage> retainedRepository, IRepository<ConnectionWill> willRepository, IRepository<ClientSubscription> subscriptionRepository)
		{
			this.clientManager = clientManager;

			this.flows = new Dictionary<ProtocolFlowType, IProtocolFlow>();

			this.flows.Add (ProtocolFlowType.Connect, new ConnectFlow (sessionRepository, willRepository));
			this.flows.Add (ProtocolFlowType.Publish, new PublishFlow (configuration, clientManager, retainedRepository, subscriptionRepository));
			this.flows.Add (ProtocolFlowType.Subscribe, new SubscribeFlow (configuration, subscriptionRepository));
			this.flows.Add (ProtocolFlowType.Unsubscribe, new UnsubscribeFlow (subscriptionRepository));
			this.flows.Add (ProtocolFlowType.Ping, new PingFlow ());
			this.flows.Add (ProtocolFlowType.Disconnect, new DisconnectFlow (clientManager, willRepository));
		}

		public void Handle (string clientId, IChannel<IPacket> channel)
		{
			this.clientManager.Add (clientId, channel);

			channel.Receiver.Subscribe (async packet => {
				var flowType = packet.Type.ToFlowType ();
				var flow = default (IProtocolFlow);

				if (!this.flows.TryGetValue (flowType, out flow)) {
					var error = string.Format (Resources.ProtocolFlowProvider_UnknownPacketType, packet.Type);
				
					throw new ProtocolException (error);
				}

				await flow.ExecuteAsync (clientId, packet, channel);
			});
		}
	}
}
