using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Hermes.Flows;
using Hermes.Packets;
using Hermes.Properties;

namespace Hermes
{
	public class CommunicationHandler : ICommunicationHandler
	{
		readonly IClientManager clientManager;
		readonly IProtocolFlowProvider flowProvider;
		readonly ProtocolConfiguration configuration;

		public CommunicationHandler (IClientManager clientManager,
			IProtocolFlowProvider flowProvider,
			ProtocolConfiguration configuration)
		{
			this.clientManager = clientManager;
			this.flowProvider = flowProvider;
			this.configuration = configuration;
		}

		public ICommunicationContext Handle (IChannel<IPacket> channel)
		{
			var context = new CommunicationContext ();
			var clientId = string.Empty;
			var keepAlive = 0;

			var packetDueTime = new TimeSpan (0, 0, this.configuration.WaitingTimeoutSecs);

			channel.Receiver
				.FirstAsync ()
				.Timeout (packetDueTime)
				.Subscribe (async packet => {
					var connect = packet as Connect;

					if (connect == null) {
						context.PushError (Resources.CommunicationHandler_FirstPacketMustBeConnect);
						return;
					}

					clientId = connect.ClientId;
					keepAlive = connect.KeepAlive;
					this.clientManager.AddClient (clientId, channel);

					await this.DispatchPacketAsync (connect, clientId, context);

					channel.Receiver
						.Skip (1)
						.Timeout (GetKeepAliveTolerance (keepAlive))
						.Subscribe (_ => { }, ex => {
							var message = string.Format (Resources.CommunicationHandler_KeepAliveTimeExceeded, keepAlive);

							context.PushError (message, ex);
						});
				}, ex => {
					context.PushError (Resources.CommunicationHandler_NoConnectReceived, ex);
				});

			channel.Receiver
				.Skip (1)
				.Subscribe (async packet => {
					if (context.IsFaulted)
						return;

					if (packet is Connect) {
						context.PushError (Resources.CommunicationHandler_SecondConnectNotAllowed);
						return;
					}

					await this.DispatchPacketAsync (packet, clientId, context);
				});

			return context;
		}

		private async Task DispatchPacketAsync (IPacket packet, string clientId, ICommunicationContext context)
		{
			var flow = this.flowProvider.GetFlow (packet.Type);
			if (flow != null)
				await flow.ExecuteAsync (clientId, packet, context);
		}

		private static TimeSpan GetKeepAliveTolerance (int keepAlive)
		{
			if (keepAlive == 0)
				keepAlive = 2 ^ 32 - 2; //Max accepted value of TimeSpan
			else
				keepAlive = (int)(keepAlive * 1.5);

			return new TimeSpan (0, 0, keepAlive);
		}
	}
}
