using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Hermes.Diagnostics;
using Hermes.Flows;
using Hermes.Packets;
using Hermes.Properties;

namespace Hermes
{
	public class ServerPacketListener : IPacketListener
	{
		static readonly ITracer tracer = Tracer.Get<ServerPacketListener> ();

		IDisposable firstPacketSubscription;
		IDisposable nextPacketsSubscription;
		IDisposable allPacketsSubscription;
		IDisposable senderSubscription;
		IDisposable keepAliveSubscription;

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
			var packetDueTime = TimeSpan.FromSeconds(this.configuration.WaitingTimeoutSecs);

			this.firstPacketSubscription = channel.Receiver
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

					tracer.Info (Resources.Tracer_ServerPacketListener_ConnectPacketReceived, clientId);

					await this.DispatchPacketAsync (connect, clientId, channel);
				}, async ex => {
					await this.HandleConnectionExceptionAsync (ex, channel);
				});

			this.nextPacketsSubscription = channel.Receiver
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

			this.allPacketsSubscription = channel.Receiver.Subscribe (_ => { }, () => {
				tracer.Warn (Resources.Tracer_PacketChannelCompleted, clientId);

				if (!string.IsNullOrEmpty (clientId)) {
					this.RemoveClient (clientId);
				}
				
				this.packets.OnCompleted ();	
			});

			this.senderSubscription = channel.Sender
				.OfType<ConnectAck> ()
				.FirstAsync ()
				.Subscribe (connectAck => {
					if (keepAlive > 0) {
						this.MonitorKeepAliveAsync (channel, clientId, keepAlive);
					}
				});
		}

		private async Task HandleConnectionExceptionAsync(Exception exception, IChannel<IPacket> channel)
		{
			if (exception is TimeoutException) {
				this.NotifyError (Resources.ServerPacketListener_NoConnectReceived, exception);
			} else if (exception is ProtocolConnectionException) {
				var connectEx = exception as ProtocolConnectionException;
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

		private void MonitorKeepAliveAsync(IChannel<IPacket> channel, string clientId, int keepAlive)
		{
			var tolerance = GetKeepAliveTolerance (keepAlive);

			this.keepAliveSubscription = channel.Receiver
				.Timeout (tolerance)
				.Subscribe (_ => { }, ex => {
					var timeEx = ex as TimeoutException;

					if (timeEx == null) {
						this.NotifyError (ex, clientId);
					} else {
						var message = string.Format (Resources.ServerPacketListener_KeepAliveTimeExceeded, tolerance, clientId);

						this.NotifyError(message, timeEx, clientId);
					}
				});
		}
		
		private static TimeSpan GetKeepAliveTolerance(int keepAlive)
		{
			var tolerance = (int)Math.Round (keepAlive * 1.5, MidpointRounding.AwayFromZero);

			return TimeSpan.FromSeconds (tolerance);
		}

		private async Task DispatchPacketAsync(IPacket packet, string clientId, IChannel<IPacket> channel)
		{
			var flow = this.flowProvider.GetFlow (packet.Type);

			if (flow != null) {
				try {
					tracer.Info (Resources.Tracer_ServerPacketListener_DispatchingMessage, packet.Type, flow.GetType().Name, clientId);

					this.packets.OnNext (packet);

					await flow.ExecuteAsync (clientId, packet, channel);
				} catch (Exception ex) {
					this.NotifyError (ex, clientId);
				}
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
