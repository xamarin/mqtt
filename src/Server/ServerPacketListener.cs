using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Hermes.Flows;
using Hermes.Packets;
using Hermes.Properties;

namespace Hermes
{
	public class ServerPacketListener : IPacketListener
	{
		readonly IConnectionProvider connectionProvider;
		readonly IProtocolFlowProvider flowProvider;
		readonly ProtocolConfiguration configuration;
		readonly ReplaySubject<IPacket> packets;

		public ServerPacketListener (IConnectionProvider connectionProvider, 
			IProtocolFlowProvider flowProvider,
			ProtocolConfiguration configuration)
		{
			this.connectionProvider = connectionProvider;
			this.flowProvider = flowProvider;
			this.configuration = configuration;
			this.packets = new ReplaySubject<IPacket> (window: TimeSpan.FromSeconds(configuration.WaitingTimeoutSecs));
		}

		public IObservable<IPacket> Packets { get { return this.packets; } }

		public void Listen (IChannel<IPacket> channel)
		{
			var clientId = string.Empty;
			var keepAlive = 0;
			var packetDueTime = new TimeSpan(0, 0, this.configuration.WaitingTimeoutSecs);

			channel.Receiver
				.FirstOrDefaultAsync ()
				.Timeout (packetDueTime)
				.Subscribe(async packet => {
					if (packet == default (IPacket)) {
						return;
					}

					var connect = packet as Connect;

					if (connect == null) {
						this.NotifyError (Resources.ServerPacketListener_FirstPacketMustBeConnect);
						return;
					}

					clientId = connect.ClientId;
					keepAlive = connect.KeepAlive;
					this.connectionProvider.AddConnection (clientId, channel);

					await this.DispatchPacketAsync (connect, clientId, channel);
				}, async ex => {
					await this.HandleConnectionExceptionAsync (ex, channel);
				});

			channel.Receiver
				.Skip (1)
				.Subscribe (async packet => {
					if (packet is Connect) {
						this.NotifyError (Resources.ServerPacketListener_SecondConnectNotAllowed, clientId);
						return;
					}

					await this.DispatchPacketAsync (packet, clientId, channel);
				}, ex => {
					this.NotifyError (ex, clientId);
				});

			channel.Receiver.Subscribe (_ => { }, () => {
				if (!string.IsNullOrEmpty (clientId)) {
					this.RemoveClient (clientId);
				}
				
				this.packets.OnCompleted ();	
			});

			channel.Sender
				.OfType<ConnectAck> ()
				.FirstAsync ()
				.Subscribe (async connectAck => {
					if (keepAlive > 0) {
						await this.MonitorKeepAliveAsync (channel, clientId, keepAlive);
					}
				});
		}

		private async Task HandleConnectionExceptionAsync(Exception exception, IChannel<IPacket> channel)
		{
			if (exception is TimeoutException) {
				this.NotifyError (Resources.ServerPacketListener_NoConnectReceived, exception);
			} else if (exception is ConnectProtocolException) {
				var connectEx = exception as ConnectProtocolException;
				var errorAck = new ConnectAck (connectEx.ReturnCode, existingSession: false);

				try {
					await channel.SendAsync (errorAck);
				} catch (Exception ex) {
					this.NotifyError (ex);
				}

				this.NotifyError (exception.Message, exception);
			} else {
				this.NotifyError (exception);
			}
		}

		private async Task DispatchPacketAsync(IPacket packet, string clientId, IChannel<IPacket> channel)
		{
			var flow = this.flowProvider.GetFlow (packet.Type);

			if (flow != null) {
				try {
					this.packets.OnNext (packet);

					await flow.ExecuteAsync (clientId, packet, channel);
				} catch (Exception ex) {
					this.NotifyError (ex, clientId);
				}
			}
		}

		private static TimeSpan GetKeepAliveTolerance(int keepAlive)
		{
			keepAlive = (int)(keepAlive * 1.5);

			return new TimeSpan (0, 0, keepAlive);
		}

		private async Task MonitorKeepAliveAsync(IChannel<IPacket> channel, string clientId, int keepAlive)
		{
			try {
				var packet = await channel.Receiver.Timeout (GetKeepAliveTolerance(keepAlive));
			} catch(TimeoutException timeEx) {
				var message = string.Format (Resources.ServerPacketListener_KeepAliveTimeExceeded, keepAlive);

				this.NotifyError(message, timeEx, clientId);
			} catch(Exception ex) {
				this.NotifyError (ex, clientId);
			}
		}

		private void NotifyError(Exception exception, string clientId = null)
		{
			if (!string.IsNullOrEmpty (clientId)) {
				this.RemoveClient (clientId);
			}
			
			this.packets.OnError (exception);
		}

		private void NotifyError(string message, string clientId = null)
		{
			this.NotifyError (new ProtocolException (message), clientId);
		}

		private void NotifyError(string message, Exception exception, string clientId = null)
		{
			this.NotifyError (new ProtocolException (message, exception), clientId);
		}

		private void RemoveClient(string clientId)
		{
			this.connectionProvider.RemoveConnection (clientId);
		}
	}
}
