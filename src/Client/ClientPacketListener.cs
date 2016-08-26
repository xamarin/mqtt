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
		readonly ITracer tracer;
		readonly IChannel<IPacket> channel;
		readonly IProtocolFlowProvider flowProvider;
		readonly ProtocolConfiguration configuration;
		readonly ReplaySubject<IPacket> packets;
		readonly TaskRunner flowRunner;
		IDisposable disposable;
		bool disposed;
		string clientId = string.Empty;
		Timer keepAliveTimer;

		public ClientPacketListener (IChannel<IPacket> channel, 
			IProtocolFlowProvider flowProvider, 
			ITracerManager tracerManager,
			ProtocolConfiguration configuration)
		{
			tracer = tracerManager.Get<ClientPacketListener> ();
			this.channel = channel;
			this.flowProvider = flowProvider;
			this.configuration = configuration;
			packets = new ReplaySubject<IPacket> (window: TimeSpan.FromSeconds (configuration.WaitingTimeoutSecs));
			flowRunner = TaskRunner.Get ();
		}

		public IObservable<IPacket> Packets { get { return packets; } }

		public void Listen ()
		{
			if (disposed) {
				throw new ObjectDisposedException (GetType ().FullName);
			}

			disposable = new CompositeDisposable (
				ListenFirstPacket (),
				ListenNextPackets (),
				ListenCompletionAndErrors (),
				ListenSentConnectPacket (),
				ListenSentDisconnectPacket ());
		}

		public void Dispose ()
		{
			Dispose (disposing: true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposed) {
				return;
			}

			if (disposing) {
				tracer.Info (Resources.Tracer_Disposing, GetType ().FullName);

				disposable.Dispose ();
				StopKeepAliveMonitor ();
				packets.OnCompleted ();
				(flowRunner as IDisposable)?.Dispose ();
				disposed = true;
			}
		}

		IDisposable ListenFirstPacket ()
		{
			return channel.Receiver
				.FirstOrDefaultAsync ()
				.Subscribe (async packet => {
					if (packet == default (IPacket)) {
						return;
					}

					tracer.Info (Resources.Tracer_ClientPacketListener_FirstPacketReceived, clientId, packet.Type);

					var connectAck = packet as ConnectAck;

					if (connectAck == null) {
						NotifyError (Resources.ClientPacketListener_FirstReceivedPacketMustBeConnectAck);
						return;
					}

					await DispatchPacketAsync (packet)
						.ConfigureAwait (continueOnCapturedContext: false);
				}, ex => {
					NotifyError (ex);
				});
		}

		IDisposable ListenNextPackets ()
		{
			return channel.Receiver
				.Skip (1)
				.Subscribe (async packet => {
					await DispatchPacketAsync (packet)
						.ConfigureAwait (continueOnCapturedContext: false);
				}, ex => {
					NotifyError (ex);
				});
		}

		IDisposable ListenCompletionAndErrors ()
		{
			return channel.Receiver.Subscribe (_ => { },
				ex => {
					NotifyError (ex);
				}, () => {
					tracer.Warn (Resources.Tracer_PacketChannelCompleted, clientId);

					packets.OnCompleted ();
				});
		}

		IDisposable ListenSentConnectPacket ()
		{
			return channel.Sender
				.OfType<Connect> ()
				.FirstAsync ()
				.ObserveOn (NewThreadScheduler.Default)
				.Subscribe (connect => {
					clientId = connect.ClientId;

					if (configuration.KeepAliveSecs > 0) {
						StartKeepAliveMonitor ();
					}
				});
		}

		IDisposable ListenSentDisconnectPacket ()
		{
			return channel.Sender
				.OfType<Disconnect> ()
				.FirstAsync ()
				.ObserveOn (NewThreadScheduler.Default)
				.Subscribe (disconnect => {
					if (configuration.KeepAliveSecs > 0) {
						StopKeepAliveMonitor ();
					}
				});
		}

		void StartKeepAliveMonitor ()
		{
			var interval = configuration.KeepAliveSecs * 1000;

			keepAliveTimer = new Timer ();

			keepAliveTimer.AutoReset = true;
			keepAliveTimer.IntervalMillisecs = interval;
			keepAliveTimer.Elapsed += async (sender, e) => {
				try {
					tracer.Warn (Resources.Tracer_ClientPacketListener_SendingKeepAlive, clientId, configuration.KeepAliveSecs);

					var ping = new PingRequest ();

					await channel.SendAsync (ping)
						.ConfigureAwait (continueOnCapturedContext: false);
				} catch (Exception ex) {
					NotifyError (ex);
				}
			};
			keepAliveTimer.Start ();

			channel.Sender.Subscribe (p => {
				keepAliveTimer.IntervalMillisecs = interval;
			});
		}

		void StopKeepAliveMonitor ()
		{
			if (keepAliveTimer != null) {
				keepAliveTimer.Stop ();
			}
		}

		async Task DispatchPacketAsync (IPacket packet)
		{
			var flow = flowProvider.GetFlow (packet.Type);

			if (flow != null) {
				try {
					packets.OnNext (packet);

					await flowRunner.Run (async () => {
						var publish = packet as Publish;

						if (publish == null) {
							tracer.Info (Resources.Tracer_ClientPacketListener_DispatchingMessage, clientId, packet.Type, flow.GetType ().Name);
						} else {
							tracer.Info (Resources.Tracer_ClientPacketListener_DispatchingPublish, clientId, flow.GetType ().Name, publish.Topic);
						}

						await flow.ExecuteAsync (clientId, packet, channel)
							.ConfigureAwait (continueOnCapturedContext: false);
					})
					.ConfigureAwait (continueOnCapturedContext: false);
				} catch (Exception ex) {
					NotifyError (ex);
				}
			}
		}

		void NotifyError (Exception exception)
		{
			tracer.Error (exception, Resources.Tracer_ClientPacketListener_Error);

			packets.OnError (exception);
		}

		void NotifyError (string message)
		{
			NotifyError (new MqttException (message));
		}

		void NotifyError (string message, Exception exception)
		{
			NotifyError (new MqttException (message, exception));
		}
	}
}
