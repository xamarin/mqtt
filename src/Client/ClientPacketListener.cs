using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Flows;
using System.Net.Mqtt.Packets;
using System.Reactive.Disposables;
using System.Net.Mqtt.Exceptions;
using System.Reactive.Concurrency;

namespace System.Net.Mqtt.Client
{
	internal class ClientPacketListener : IPacketListener
	{
		private static readonly ITracer tracer = Tracer.Get<ClientPacketListener> ();

		readonly IChannel<IPacket> channel;
		readonly IProtocolFlowProvider flowProvider;
		readonly ProtocolConfiguration configuration;
		readonly ReplaySubject<IPacket> packets;
		readonly TaskRunner flowRunner;
		IDisposable disposable;
		bool disposed;
		string clientId = string.Empty;
		Timers.Timer keepAliveTimer;

		public ClientPacketListener (IChannel<IPacket> channel, IProtocolFlowProvider flowProvider, ProtocolConfiguration configuration)
		{
			this.channel = channel;
			this.flowProvider = flowProvider;
			this.configuration = configuration;
			this.packets = new ReplaySubject<IPacket> (window: TimeSpan.FromSeconds(configuration.WaitingTimeoutSecs));
			this.flowRunner = TaskRunner.Get ("ClientFlowRunner");
		}

		public IObservable<IPacket> Packets { get { return this.packets; } }

		public void Listen ()
		{
			if (this.disposed) {
				throw new ObjectDisposedException (this.GetType ().FullName);
			}

			this.disposable = new CompositeDisposable (
				this.ListenFirstPacket (),
				this.ListenNextPackets (),
				this.ListenCompletionAndErrors (),
				this.ListenSentConnectPacket (),
				this.ListenSentDisconnectPacket ());
		}

		public void Dispose ()
		{
			this.Dispose (disposing: true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (this.disposed) {
				return;
			}

			if (disposing) {
				tracer.Info (Properties.Resources.Tracer_Disposing, this.GetType ().FullName);

				this.disposable.Dispose ();
				this.StopKeepAliveMonitor ();
				this.packets.OnCompleted ();
				this.disposed = true;
			}
		}

		private IDisposable ListenFirstPacket()
		{
			return this.channel.Receiver
				.FirstOrDefaultAsync()
				.Subscribe(async packet => {
					if (packet == default (IPacket)) {
						return;
					}

					tracer.Info (Properties.Resources.Tracer_ClientPacketListener_FirstPacketReceived, this.clientId, packet.Type);

					var connectAck = packet as ConnectAck;

					if (connectAck == null) {
						this.NotifyError (Properties.Resources.ClientPacketListener_FirstReceivedPacketMustBeConnectAck);
						return;
					}

					await this.DispatchPacketAsync (packet)
						.ConfigureAwait(continueOnCapturedContext: false);
				}, ex => {
					this.NotifyError (ex);
				});
		}

		private IDisposable ListenNextPackets()
		{
			return this.channel.Receiver
				.Skip(1)
				.Subscribe (async packet => {
					await this.DispatchPacketAsync (packet)
						.ConfigureAwait(continueOnCapturedContext: false);
				}, ex => {
					this.NotifyError (ex);
				});
		}

		private IDisposable ListenCompletionAndErrors()
		{
			return this.channel.Receiver.Subscribe (_ => { }, 
				ex => {
					this.NotifyError (ex);
				}, () => {
					tracer.Warn (Properties.Resources.Tracer_PacketChannelCompleted, this.clientId);

					this.packets.OnCompleted ();	
				});
		}

		private IDisposable ListenSentConnectPacket()
		{
			return this.channel.Sender
				.OfType<Connect> ()
				.FirstAsync ()
				.ObserveOn(NewThreadScheduler.Default)
				.Subscribe (connect => {
					this.clientId = connect.ClientId;

					if (this.configuration.KeepAliveSecs > 0) {
						this.StartKeepAliveMonitor ();
					}
				});
		}

		private IDisposable ListenSentDisconnectPacket()
		{
			return this.channel.Sender
				.OfType<Disconnect> ()
				.FirstAsync ()
				.ObserveOn(NewThreadScheduler.Default)
				.Subscribe (disconnect => {
					if (this.configuration.KeepAliveSecs > 0) {
						this.StopKeepAliveMonitor ();
					}
				});
		}

		private void StartKeepAliveMonitor()
		{
			var interval = this.configuration.KeepAliveSecs * 1000;

			this.keepAliveTimer = new Timers.Timer();

			this.keepAliveTimer.AutoReset = true;
			this.keepAliveTimer.Interval = interval;
			this.keepAliveTimer.Elapsed += async (sender, e) => {
				try {
					tracer.Warn (Properties.Resources.Tracer_ClientPacketListener_SendingKeepAlive, this.clientId, this.configuration.KeepAliveSecs);

					var ping = new PingRequest ();

					await this.channel.SendAsync (ping)
						.ConfigureAwait(continueOnCapturedContext: false);
				} catch (Exception ex) {
					this.NotifyError (ex);
				}
			};
			this.keepAliveTimer.Start ();

			this.channel.Sender.Subscribe (p => {
				this.keepAliveTimer.Interval = interval;
			});
		}

		private void StopKeepAliveMonitor()
		{
			if (this.keepAliveTimer != null) {
				this.keepAliveTimer.Dispose ();
			}
		}

		private async Task DispatchPacketAsync(IPacket packet)
		{
			var flow = this.flowProvider.GetFlow (packet.Type);

			if (flow != null) {
				try {
					this.packets.OnNext (packet);

					await this.flowRunner.Run (async () => {
						var publish = packet as Publish;

						if (publish == null) {
							tracer.Info (Properties.Resources.Tracer_ClientPacketListener_DispatchingMessage, this.clientId, packet.Type, flow.GetType().Name);
						} else {
							tracer.Info (Properties.Resources.Tracer_ClientPacketListener_DispatchingPublish, this.clientId, flow.GetType().Name, publish.Topic);
						}

						await flow.ExecuteAsync (this.clientId, packet, this.channel)
							.ConfigureAwait(continueOnCapturedContext: false);
					})
					.ConfigureAwait(continueOnCapturedContext: false);
				} catch (Exception ex) {
					this.NotifyError (ex);
				}
			}
		}

		private void NotifyError(Exception exception)
		{
			tracer.Error (exception, Properties.Resources.Tracer_ClientPacketListener_Error);

			this.packets.OnError (exception);
		}

		private void NotifyError(string message)
		{
			this.NotifyError (new MqttException (message));
		}

		private void NotifyError(string message, Exception exception)
		{
			this.NotifyError (new MqttException (message, exception));
		}
	}
}
