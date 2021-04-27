using System.Diagnostics;
using System.Net.Mqtt.Sdk.Flows;
using System.Net.Mqtt.Sdk.Packets;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Sdk
{
	internal class ClientPacketListener : IPacketListener
	{
		static readonly ITracer tracer = Tracer.Get<ClientPacketListener> ();

		readonly IMqttChannel<IPacket> channel;
		readonly IProtocolFlowProvider flowProvider;
		readonly MqttConfiguration configuration;
		readonly ReplaySubject<IPacket> packets;
		IDisposable listenerDisposable;
		bool disposed;
		string clientId = string.Empty;
		Timer keepAliveTimer;

		public ClientPacketListener (IMqttChannel<IPacket> channel, 
			IProtocolFlowProvider flowProvider, 
			MqttConfiguration configuration)
		{
			this.channel = channel;
			this.flowProvider = flowProvider;
			this.configuration = configuration;
			packets = new ReplaySubject<IPacket> (window: TimeSpan.FromSeconds (configuration.WaitTimeoutSecs));
		}

		public IObservable<IPacket> PacketStream { get { return packets; } }

		public void Listen ()
		{
			if (disposed) {
				throw new ObjectDisposedException (GetType ().FullName);
			}

			listenerDisposable = new CompositeDisposable (
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
				tracer.Info (Properties.Resources.Mqtt_Disposing, GetType ().FullName);

				listenerDisposable.Dispose ();
				StopKeepAliveMonitor ();
				packets.OnCompleted ();
				disposed = true;
			}
		}

		IDisposable ListenFirstPacket ()
		{
			return channel
                .ReceiverStream
				.FirstOrDefaultAsync ()
				.Subscribe (async packet => {
					if (packet == default (IPacket)) {
						return;
					}

					tracer.Info (Properties.Resources.ClientPacketListener_FirstPacketReceived, clientId, packet.Type);

					var connectAck = packet as ConnectAck;

					if (connectAck == null) {
						NotifyError (Properties.Resources.ClientPacketListener_FirstReceivedPacketMustBeConnectAck);
						return;
					}

					if (configuration.KeepAliveSecs > 0) {
						StartKeepAliveMonitor ();
					}

					await DispatchPacketAsync (packet)
						.ConfigureAwait (continueOnCapturedContext: false);
				}, ex => {
					NotifyError (ex);
				});
		}

		IDisposable ListenNextPackets ()
		{
			return channel
                .ReceiverStream
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
			return channel
                .ReceiverStream
                .Subscribe (_ => { },
				    ex => {
					    NotifyError (ex);
				    }, () => {
					    tracer.Warn (Properties.Resources.ClientPacketListener_PacketChannelCompleted, clientId);

					    packets.OnCompleted ();
				    }
                );
		}

		IDisposable ListenSentConnectPacket ()
		{
			return channel
				.SenderStream
				.OfType<Connect> ()
				.FirstAsync ()
				.Subscribe (connect => {
					clientId = connect.ClientId;
				});
		}

		IDisposable ListenSentDisconnectPacket ()
		{
			return channel
				.SenderStream
				.OfType<Disconnect> ()
				.FirstAsync ()
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
					tracer.Warn (Properties.Resources.ClientPacketListener_SendingKeepAlive, clientId, configuration.KeepAliveSecs);

					var ping = new PingRequest ();

					await channel.SendAsync (ping)
						.ConfigureAwait (continueOnCapturedContext: false);
				} catch (Exception ex) {
					NotifyError (ex);
				}
			};
			keepAliveTimer.Start ();

			channel.SenderStream.Subscribe (p => {
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

					var publish = packet as Publish;

					if (publish == null)
					{
						tracer.Info(Properties.Resources.ClientPacketListener_DispatchingMessage, clientId, packet.Type, flow.GetType().Name);
					}
					else
					{
						tracer.Info(Properties.Resources.ClientPacketListener_DispatchingPublish, clientId, flow.GetType().Name, publish.Topic);
					}

					await flow
						.ExecuteAsync(clientId, packet, channel)
						.ConfigureAwait(continueOnCapturedContext: false);
				} catch (Exception ex) {
					NotifyError (ex);
				}
			}
		}

		void NotifyError (Exception exception)
		{
			tracer.Error (exception, Properties.Resources.ClientPacketListener_Error);

			packets.OnError (exception);
		}

		void NotifyError (string message)
		{
			NotifyError (new MqttException (message));
		}
	}
}
