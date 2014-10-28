using System.Collections.Generic;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class ProtocolFlowProvider : IProtocolFlowProvider
	{
		readonly IDictionary<ProtocolFlowType, IProtocolFlow> flows;

		public ProtocolFlowProvider (IProtocolConfiguration configuration, IClientManager clientManager, 
			IRepository<ProtocolSession> sessionRepository, IRepository<RetainedMessage> retainedRepository,
			IRepository<ConnectionWill> willRepository, IRepository<ConnectionRefused> connectionRefusedRepository, 
			IRepository<ClientSubscription> subscriptionRepository)
		{
			this.flows = new Dictionary<ProtocolFlowType, IProtocolFlow> ();

			this.flows.Add (ProtocolFlowType.Connect, new ConnectFlow (sessionRepository, willRepository, connectionRefusedRepository));
			this.flows.Add (ProtocolFlowType.Publish, new PublishFlow (configuration, clientManager, retainedRepository, subscriptionRepository, connectionRefusedRepository));
			this.flows.Add (ProtocolFlowType.Subscribe, new SubscribeFlow (configuration, subscriptionRepository, connectionRefusedRepository));
			this.flows.Add (ProtocolFlowType.Unsubscribe, new UnsubscribeFlow (subscriptionRepository, connectionRefusedRepository));
			this.flows.Add (ProtocolFlowType.Ping, new PingFlow (connectionRefusedRepository));
			this.flows.Add (ProtocolFlowType.Disconnect, new DisconnectFlow (sessionRepository, willRepository, connectionRefusedRepository));
		}

		public IProtocolFlow Get(PacketType packetType)
		{
			var flowType = this.GetFlowType (packetType);
			var flow = default (IProtocolFlow);

			if (!this.flows.TryGetValue (flowType, out flow)) {
				var error = string.Format (Resources.ProtocolFlowProvider_UnknownPacketType, packetType);
				
				throw new ProtocolException (error);
			}
				
			return flow;
		}

		public ProtocolFlowType GetFlowType(PacketType packetType)
		{
			var flowType = default (ProtocolFlowType);

			switch (packetType) {
				case PacketType.Connect:
				case PacketType.ConnectAck:
					flowType = ProtocolFlowType.Connect;
					break;
				case PacketType.Publish:
				case PacketType.PublishAck:
				case PacketType.PublishReceived:
				case PacketType.PublishRelease:
				case PacketType.PublishComplete:
					flowType = ProtocolFlowType.Publish;
					break;
				case PacketType.Subscribe:
				case PacketType.SubscribeAck:
					flowType = ProtocolFlowType.Subscribe;
					break;
				case PacketType.Unsubscribe:
				case PacketType.UnsubscribeAck:
					flowType = ProtocolFlowType.Unsubscribe;
					break;
				case PacketType.PingRequest:
				case PacketType.PingResponse:
					flowType = ProtocolFlowType.Ping;
					break;
				case PacketType.Disconnect:
					flowType = ProtocolFlowType.Disconnect;
					break;
			}

			return flowType;
		}
	}
}
