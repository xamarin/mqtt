using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Hermes.Flows;
using Hermes.Packets;
using Hermes.Properties;

namespace Hermes
{
	public class ServerPacketChannelAdapter : IPacketChannelAdapter
	{
		readonly IConnectionProvider connectionProvider;
		readonly IProtocolFlowProvider flowProvider;
		readonly ProtocolConfiguration configuration;

		public ServerPacketChannelAdapter (IConnectionProvider connectionProvider, 
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
			var keepAlive = 0;

			var packetDueTime = new TimeSpan(0, 0, this.configuration.WaitingTimeoutSecs);

			protocolChannel.Receiver
				.FirstAsync ()
				.Timeout (packetDueTime)
				.Subscribe(async packet => {
					var connect = packet as Connect;

					if (connect == null) {
						protocolChannel.NotifyError (Resources.ServerPacketChannelAdapter_FirstPacketMustBeConnect);
						return;
					}

					clientId = connect.ClientId;
					keepAlive = connect.KeepAlive;
					this.connectionProvider.AddConnection (clientId, protocolChannel);

					await this.DispatchPacketAsync (connect, clientId, protocolChannel);
					
					if (keepAlive > 0) {
						protocolChannel.Receiver
							.Skip (1)
							.Timeout (GetKeepAliveTolerance(keepAlive))
							.Subscribe(_ => {}, ex => {
								var message = string.Format (Resources.ServerPacketChannelAdapter_KeepAliveTimeExceeded, keepAlive);

								this.NotifyError(message, ex, clientId, protocolChannel);
							});
					}
				}, async ex => {
					await this.HandleConnectionExceptionAsync (ex, protocolChannel);
				});

			protocolChannel.Receiver
				.Skip (1)
				.Subscribe (async packet => {
					if (packet is Connect) {
						this.NotifyError (Resources.ServerPacketChannelAdapter_SecondConnectNotAllowed, clientId, protocolChannel);
						return;
					}

					await this.DispatchPacketAsync (packet, clientId, protocolChannel);
				}, ex => {
					this.NotifyError (ex, clientId, protocolChannel);
				}, () => {
					this.connectionProvider.RemoveConnection (clientId);
				});

			return protocolChannel;
		}

		private async Task HandleConnectionExceptionAsync(Exception ex, ProtocolChannel channel)
		{
			if (ex is TimeoutException) {
				channel.NotifyError (Resources.ServerPacketChannelAdapter_NoConnectReceived, ex);
			} else if (ex is ConnectProtocolException) {
				var connectEx = ex as ConnectProtocolException;
				var errorAck = new ConnectAck (connectEx.ReturnCode, existingSession: false);

				await channel.SendAsync (errorAck);

				channel.NotifyError (ex.Message, ex);
			} else {
				channel.NotifyError (ex);
			}
		}

		private async Task DispatchPacketAsync(IPacket packet, string clientId, ProtocolChannel channel)
		{
			var flow = this.flowProvider.GetFlow (packet.Type);

			if (flow != null) {
				try {
					await flow.ExecuteAsync (clientId, packet);
				} catch (Exception ex) {
					this.NotifyError (ex, clientId, channel);
				}
			}
		}

		private static TimeSpan GetKeepAliveTolerance(int keepAlive)
		{
			keepAlive = (int)(keepAlive * 1.5);

			return new TimeSpan (0, 0, keepAlive);
		}

		public void NotifyError(Exception exception, string clientId, ProtocolChannel channel)
		{
			this.RemoveClient (clientId);
			channel.NotifyError (exception);
		}

		public void NotifyError(string message, string clientId, ProtocolChannel channel)
		{
			this.RemoveClient (clientId);
			channel.NotifyError (message);
		}

		public void NotifyError(string message, Exception exception, string clientId, ProtocolChannel channel)
		{
			this.RemoveClient (clientId);
			channel.NotifyError (message, exception);
		}

		private void RemoveClient(string clientId)
		{
			this.connectionProvider.RemoveConnection (clientId);
		}
	}
}
