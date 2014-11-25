using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Hermes.Flows;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes
{
	public class PacketChannelAdapter : IPacketChannelAdapter
	{
		readonly IClientManager clientManager;
		readonly IProtocolFlowProvider flowProvider;
		readonly ProtocolConfiguration configuration;

		public PacketChannelAdapter (IRepositoryFactory repositoryFactory, ProtocolConfiguration configuration)
			: this(new ClientManager(), new ProtocolFlowProvider(repositoryFactory, configuration), configuration)
		{
		}

		public PacketChannelAdapter (IClientManager clientManager, 
			IProtocolFlowProvider flowProvider,
			ProtocolConfiguration configuration)
		{
			this.clientManager = clientManager;
			this.flowProvider = flowProvider;
			this.configuration = configuration;
		}

		public IChannel<IPacket> Adapt (IChannel<IPacket> channel)
		{
			var protocolChannel = new ProtocolChannel (channel);
			var clientId = string.Empty;
			var keepAlive = 0;

			var packetDueTime = new TimeSpan(0, 0, this.configuration.WaitingTimeoutSecs);

			channel.Receiver
				.FirstAsync ()
				.Timeout (packetDueTime)
				.Subscribe(async packet => {
					var connect = packet as Connect;

					if (connect == null) {
						protocolChannel.NotifyError (Resources.PacketChannelAdapter_FirstPacketMustBeConnect);
						return;
					}

					clientId = connect.ClientId;
					keepAlive = connect.KeepAlive;
					this.clientManager.AddClient (clientId, channel);

					await this.DispatchPacketAsync (connect, clientId, protocolChannel);

					channel.Receiver
						.Skip (1)
						.Timeout (GetKeepAliveTolerance(keepAlive))
						.Subscribe(_ => {}, ex => {
							var message = string.Format (Resources.PacketChannelAdapter_KeepAliveTimeExceeded, keepAlive);

							protocolChannel.NotifyError (message, ex);	
						});
				}, ex => {
					protocolChannel.NotifyError (Resources.PacketChannelAdapter_NoConnectReceived, ex);	
				});

			channel.Receiver
				.Skip (1)
				.Subscribe (async packet => {
					if (packet is Connect) {
						protocolChannel.NotifyError (Resources.PacketChannelAdapter_SecondConnectNotAllowed);
						return;
					}

					await this.DispatchPacketAsync (packet, clientId, protocolChannel);
				});

			return protocolChannel;
		}

		private async Task DispatchPacketAsync(IPacket packet, string clientId, IChannel<IPacket> channel)
		{
			var flow = this.flowProvider.GetFlow (packet.Type);

			await flow.ExecuteAsync (clientId, packet, channel);
		}

		private static TimeSpan GetKeepAliveTolerance(int keepAlive)
		{
			if (keepAlive == 0)
				keepAlive = 2 ^ 32 - 2; //Max accepted value of TimeSpan
			else
				keepAlive = (int)(keepAlive * 1.5);

			return new TimeSpan (0, 0, keepAlive);
		}
	}
}
