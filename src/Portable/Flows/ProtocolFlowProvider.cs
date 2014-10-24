using System.Collections.Generic;
using Hermes.Messages;
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

		public IProtocolFlow Get(MessageType messageType)
		{
			var flowType = this.GetFlowType (messageType);
			var flow = default (IProtocolFlow);

			if (!this.flows.TryGetValue (flowType, out flow)) {
				var error = string.Format (Resources.ProtocolFlowProvider_UnknownMessageType, messageType);
				
				throw new ProtocolException (error);
			}
				
			return flow;
		}

		public ProtocolFlowType GetFlowType(MessageType messageType)
		{
			var flowType = default (ProtocolFlowType);

			switch (messageType) {
				case MessageType.Connect:
				case MessageType.ConnectAck:
					flowType = ProtocolFlowType.Connect;
					break;
				case MessageType.Publish:
				case MessageType.PublishAck:
				case MessageType.PublishReceived:
				case MessageType.PublishRelease:
				case MessageType.PublishComplete:
					flowType = ProtocolFlowType.Publish;
					break;
				case MessageType.Subscribe:
				case MessageType.SubscribeAck:
					flowType = ProtocolFlowType.Subscribe;
					break;
				case MessageType.Unsubscribe:
				case MessageType.UnsubscribeAck:
					flowType = ProtocolFlowType.Unsubscribe;
					break;
				case MessageType.PingRequest:
				case MessageType.PingResponse:
					flowType = ProtocolFlowType.Ping;
					break;
				case MessageType.Disconnect:
					flowType = ProtocolFlowType.Disconnect;
					break;
			}

			return flowType;
		}
	}
}
