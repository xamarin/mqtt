using System.Collections.Generic;
using Hermes.Packets;
using Hermes.Properties;

namespace Hermes.Flows
{
	public class ProtocolFlowProvider
	{
		readonly IDictionary<ProtocolFlowType, IProtocolFlow> flows;

		public ProtocolFlowProvider ()
		{
			this.flows = new Dictionary<ProtocolFlowType, IProtocolFlow> ();

			this.flows.Add (ProtocolFlowType.Connect, new ConnectFlow ());
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
