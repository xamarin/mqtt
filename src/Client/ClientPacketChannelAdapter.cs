using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Hermes.Flows;
using Hermes.Packets;
using Hermes.Properties;

namespace Hermes
{
	public class ClientPacketChannelAdapter : IPacketChannelAdapter
	{
		readonly IConnectionProvider connectionProvider;
		readonly IProtocolFlowProvider flowProvider;
		readonly ProtocolConfiguration configuration;

		public ClientPacketChannelAdapter (IConnectionProvider connectionProvider, 
			IProtocolFlowProvider flowProvider,
			ProtocolConfiguration configuration)
		{
			this.connectionProvider = connectionProvider;
			this.flowProvider = flowProvider;
			this.configuration = configuration;
		}

		public IChannel<IPacket> Adapt (IChannel<IPacket> channel)
		{
			var protocolChannel = new ProtocolChannel (channel);
			var clientId = string.Empty;

			protocolChannel.Sender
				.OfType<Connect> ()
				.FirstAsync ()
				.Subscribe (connect => {
					clientId = connect.ClientId;

					this.connectionProvider.AddConnection (clientId, protocolChannel);

					var packetDueTime = new TimeSpan(0, 0, this.configuration.WaitingTimeoutSecs);

					protocolChannel.Receiver
						.FirstAsync ()
						.Timeout (packetDueTime)
						.Subscribe(async packet => {
							var connectAck = packet as ConnectAck;

							if (connectAck == null) {
								protocolChannel.NotifyError (Resources.ClientPacketChannelAdapter_FirstReceivedPacketMustBeConnectAck);
								return;
							}

							await this.DispatchPacketAsync (connectAck, clientId, protocolChannel);

							if (this.configuration.KeepAliveSecs > 0) {
								protocolChannel.Receiver
									.Skip (1)
									.Timeout (new TimeSpan (0, 0, this.configuration.KeepAliveSecs))
									.Subscribe(_ => {}, async ex => {
										if (ex is TimeoutException) {
											var ping = new PingRequest ();

											await protocolChannel.SendAsync(ping);
										} else {
											protocolChannel.NotifyError (ex);
										}
									});
							}
						}, ex => {
							if (ex is TimeoutException) {
								protocolChannel.NotifyError (Resources.ClientPacketChannelAdapter_NoConnectAckReceived, ex);
							} else {
								protocolChannel.NotifyError (ex);
							}
						});
				});

			protocolChannel.Receiver
				.Skip (1)
				.Subscribe (async packet => {
					await this.DispatchPacketAsync (packet, clientId, protocolChannel);
				}, ex => {
					protocolChannel.NotifyError (ex);
				});

			return protocolChannel;
		}

		private async Task DispatchPacketAsync(IPacket packet, string clientId, ProtocolChannel channel)
		{
			var flow = this.flowProvider.GetFlow (packet.Type);

			if (flow != null) {
				try {
					await flow.ExecuteAsync (clientId, packet);
				} catch (Exception ex) {
					channel.NotifyError (ex);
				}
			}
		}
	}
}
