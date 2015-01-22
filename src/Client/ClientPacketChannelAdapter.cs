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
		readonly IProtocolFlowProvider flowProvider;
		readonly ProtocolConfiguration configuration;

		public ClientPacketChannelAdapter (IProtocolFlowProvider flowProvider, ProtocolConfiguration configuration)
		{
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
				});

			protocolChannel.Receiver
				.FirstAsync()
				.Subscribe(async packet => {
					var connectAck = packet as ConnectAck;

					if (connectAck == null) {
						protocolChannel.NotifyError (Resources.ClientPacketChannelAdapter_FirstReceivedPacketMustBeConnectAck);
						return;
					}

					if (this.configuration.KeepAliveSecs > 0) {
						this.MaintainKeepAlive (protocolChannel);
					}

					await this.DispatchPacketAsync (packet, clientId, protocolChannel);
				}, ex => {
					protocolChannel.NotifyError (ex);
			});

			protocolChannel.Receiver
				.Skip(1)
				.Subscribe (async packet => {
					await this.DispatchPacketAsync (packet, clientId, protocolChannel);
				}, ex => {
					protocolChannel.NotifyError (ex);
				});

			return protocolChannel;
		}

		private void MaintainKeepAlive(ProtocolChannel channel)
		{
			channel.Sender
				.Timeout (new TimeSpan (0, 0, this.configuration.KeepAliveSecs))
				.Subscribe(_ => {}, async ex => {
					if (ex is TimeoutException) {
						var ping = new PingRequest ();

						await channel.SendAsync(ping);
					} else {
						channel.NotifyError (ex);
					}
				});
		}

		private async Task DispatchPacketAsync(IPacket packet, string clientId, ProtocolChannel channel)
		{
			var flow = this.flowProvider.GetFlow (packet.Type);

			if (flow != null) {
				try {
					await flow.ExecuteAsync (clientId, packet, channel);
				} catch (Exception ex) {
					channel.NotifyError (ex);
				}
			}
		}
	}
}
