using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Hermes.Diagnostics;
using Hermes.Flows;
using Hermes.Packets;
using Hermes.Properties;

namespace Hermes
{
	public class ClientPacketListener : IPacketListener
	{
		private static readonly ITracer tracer = Tracer.Get<ClientPacketListener> ();

		IDisposable firstPacketSubscription;
		IDisposable nextPacketsSubscription;
		IDisposable allPacketsSubscription;
		IDisposable senderSubscription;
		IDisposable keepAliveSubscription;

		readonly IProtocolFlowProvider flowProvider;
		readonly ProtocolConfiguration configuration;
		readonly ReplaySubject<IPacket> packets;

		public ClientPacketListener (IProtocolFlowProvider flowProvider, ProtocolConfiguration configuration)
		{
			this.flowProvider = flowProvider;
			this.configuration = configuration;
			this.packets = new ReplaySubject<IPacket> (window: TimeSpan.FromSeconds(configuration.WaitingTimeoutSecs));
		}

		public IObservable<IPacket> Packets { get { return this.packets; } }

		public void Listen (IChannel<IPacket> channel)
		{
			var clientId = string.Empty;

			this.firstPacketSubscription = channel.Receiver
				.FirstOrDefaultAsync()
				.SubscribeOn(Scheduler.Default)
				.Subscribe(async packet => {
					if (packet == default (IPacket)) {
						return;
					}

					tracer.Info (Resources.Tracer_ClientPacketListener_FirstPacketReceived, clientId, DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff"), packet.Type);

					var connectAck = packet as ConnectAck;

					if (connectAck == null) {
						this.NotifyError (Resources.ClientPacketListener_FirstReceivedPacketMustBeConnectAck);
						return;
					}

					await this.DispatchPacketAsync (packet, clientId, channel);
				}, ex => {
					this.NotifyError (ex);
				});

			this.nextPacketsSubscription = channel.Receiver
				.Skip(1)
				.Subscribe (async packet => {
					await this.DispatchPacketAsync (packet, clientId, channel);
				}, ex => {
					this.NotifyError (ex);
				});

			this.allPacketsSubscription = channel.Receiver.Subscribe (_ => { }, () => {
				tracer.Warn (Resources.Tracer_PacketChannelCompleted, clientId, DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff"));

				this.packets.OnCompleted ();	
			});

			this.senderSubscription = channel.Sender
				.OfType<Connect> ()
				.FirstAsync ()
				.Subscribe (connect => {
					clientId = connect.ClientId;

					if (this.configuration.KeepAliveSecs > 0) {
						this.MaintainKeepAlive (channel, clientId);
					}
				});
		}

		private void MaintainKeepAlive(IChannel<IPacket> channel, string clientId)
		{
			this.keepAliveSubscription = this.GetTimeoutMonitor(channel, clientId)
				.SubscribeOn(Scheduler.Default)
				.Subscribe(_ => {}, ex => {
					this.NotifyError (ex);
				});
		}

		private IObservable<IPacket> GetTimeoutMonitor(IChannel<IPacket> channel, string clientId)
		{
			return channel.Sender
				.Timeout (TimeSpan.FromSeconds (this.configuration.KeepAliveSecs))
				.SubscribeOn(Scheduler.Default)
				.Catch<IPacket, TimeoutException> (timeEx => {
					tracer.Warn (Resources.Tracer_ClientPacketListener_SendingKeepAlive, clientId, DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff"), this.configuration.KeepAliveSecs);

					var ping = new PingRequest ();

					channel.SendAsync (ping).Wait ();

					return this.GetTimeoutMonitor (channel, clientId);
				});
		}

		private async Task DispatchPacketAsync(IPacket packet, string clientId, IChannel<IPacket> channel)
		{
			var flow = this.flowProvider.GetFlow (packet.Type);

			if (flow != null) {
				try {
					tracer.Info (Resources.Tracer_ClientPacketListener_DispatchingMessage, clientId, DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff"), packet.Type, flow.GetType().Name);

					this.packets.OnNext (packet);

					await flow.ExecuteAsync (clientId, packet, channel);
				} catch (Exception ex) {
					this.NotifyError (ex);
				}
			}
		}

		private void NotifyError(Exception exception)
		{
			this.packets.OnError (exception);
		}

		private void NotifyError(string message)
		{
			this.NotifyError (new ProtocolException (message));
		}

		private void NotifyError(string message, Exception exception)
		{
			this.NotifyError (new ProtocolException (message, exception));
		}
	}
}
