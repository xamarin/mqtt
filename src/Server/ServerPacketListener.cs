using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mqtt.Exceptions;
using System.Net.Mqtt.Flows;
using System.Net.Mqtt.Packets;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server
{
    internal class ServerPacketListener : IPacketListener
	{
		static readonly ITracer tracer = Tracer.Get<ServerPacketListener> ();

		readonly IMqttChannel<IPacket> channel;
		readonly IConnectionProvider connectionProvider;
		readonly IProtocolFlowProvider flowProvider;
		readonly MqttConfiguration configuration;
		readonly ReplaySubject<IPacket> packets;
		readonly TaskRunner flowRunner;
		CompositeDisposable disposable;
		bool disposed;
		string clientId = string.Empty;
		int keepAlive = 0;

		public ServerPacketListener (IMqttChannel<IPacket> channel,
			IConnectionProvider connectionProvider,
			IProtocolFlowProvider flowProvider,
			MqttConfiguration configuration)
		{
			this.channel = channel;
			this.connectionProvider = connectionProvider;
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
				ListenSentPackets ());
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
				packets.OnCompleted ();
				(flowRunner as IDisposable)?.Dispose ();
				disposed = true;
			}
		}

		IDisposable ListenFirstPacket ()
		{
			var packetDueTime = TimeSpan.FromSeconds(configuration.WaitingTimeoutSecs);

			return channel.Receiver
				.FirstOrDefaultAsync ()
				.Timeout (packetDueTime)
				.Subscribe (async packet => {
					if (packet == default (IPacket)) {
						return;
					}

					var connect = packet as Connect;

					if (connect == null) {
						NotifyError (Resources.ServerPacketListener_FirstPacketMustBeConnect);
						return;
					}

					clientId = connect.ClientId;
					keepAlive = connect.KeepAlive;
					connectionProvider.AddConnection (clientId, channel);

					tracer.Info (Resources.Tracer_ServerPacketListener_ConnectPacketReceived, clientId);

					await DispatchPacketAsync (connect)
						.ConfigureAwait (continueOnCapturedContext: false);
				}, async ex => {
					await HandleConnectionExceptionAsync (ex)
						.ConfigureAwait (continueOnCapturedContext: false);
				});
		}

		IDisposable ListenNextPackets ()
		{
			return channel.Receiver
				.Skip (1)
				.Subscribe (async packet => {
					if (packet is Connect) {
						NotifyError (Resources.ServerPacketListener_SecondConnectNotAllowed);
						return;
					}

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
				}, async () => {
					tracer.Warn (Resources.Tracer_PacketChannelCompleted, clientId);

					if (!string.IsNullOrEmpty (clientId)) {
						RemoveClient ();

						var publishFlow = flowProvider.GetFlow<ServerPublishReceiverFlow> ();

						await publishFlow.SendWillAsync (clientId)
							.ConfigureAwait (continueOnCapturedContext: false);
					}

					packets.OnCompleted ();
				});
		}

		IDisposable ListenSentPackets ()
		{
			return channel.Sender
				.OfType<ConnectAck> ()
				.FirstAsync ()
				.Subscribe (connectAck => {
					if (keepAlive > 0) {
						MonitorKeepAliveAsync ();
					}
				});
		}

		async Task HandleConnectionExceptionAsync (Exception exception)
		{
			if (exception is TimeoutException) {
				NotifyError (Resources.ServerPacketListener_NoConnectReceived, exception);
			} else if (exception is MqttConnectionException) {
				tracer.Error (exception, Resources.Tracer_ServerPacketListener_ConnectionError, clientId ?? "N/A");

				var connectEx = exception as MqttConnectionException;
				var errorAck = new ConnectAck (connectEx.ReturnCode, existingSession: false);

				try {
					await channel.SendAsync (errorAck)
						.ConfigureAwait (continueOnCapturedContext: false);
				} catch (Exception ex) {
					NotifyError (ex);
				}
			} else {
				NotifyError (exception);
			}
		}

		void MonitorKeepAliveAsync ()
		{
			var tolerance = GetKeepAliveTolerance ();

			var keepAliveSubscription = channel.Receiver
				.Timeout (tolerance)
				.Subscribe (_ => { }, ex => {
					var timeEx = ex as TimeoutException;

					if (timeEx == null) {
						NotifyError (ex);
					} else {
						var message = string.Format (Resources.ServerPacketListener_KeepAliveTimeExceeded, tolerance, clientId);

						NotifyError(message, timeEx);
					}
				});

			disposable.Add (keepAliveSubscription);
		}

		TimeSpan GetKeepAliveTolerance ()
		{
			var tolerance = (int)Math.Round (keepAlive * 1.5, MidpointRounding.AwayFromZero);

			return TimeSpan.FromSeconds (tolerance);
		}

		async Task DispatchPacketAsync (IPacket packet)
		{
			var flow = flowProvider.GetFlow (packet.Type);

			if (flow == null) {
				return;
			}

			try {
				packets.OnNext (packet);

				await flowRunner.Run (async () => {
					if (packet.Type == MqttPacketType.Publish) {
						var publish = packet as Publish;

						tracer.Info (Resources.Tracer_ServerPacketListener_DispatchingPublish, flow.GetType ().Name, clientId, publish.Topic);
					} else if (packet.Type == MqttPacketType.Subscribe) {
						var subscribe = packet as Subscribe;
						var topics = subscribe.Subscriptions == null ? new List<string> () : subscribe.Subscriptions.Select (s => s.TopicFilter);

						tracer.Info (Resources.Tracer_ServerPacketListener_DispatchingSubscribe, flow.GetType ().Name, clientId, string.Join (", ", topics));
					} else {
						tracer.Info (Resources.Tracer_ServerPacketListener_DispatchingMessage, packet.Type, flow.GetType ().Name, clientId);
					}

					await flow.ExecuteAsync (clientId, packet, channel)
						.ConfigureAwait (continueOnCapturedContext: false);
				})
				.ConfigureAwait (continueOnCapturedContext: false);
			} catch (Exception ex) {
				if (flow is ServerConnectFlow) {
					HandleConnectionExceptionAsync (ex).Wait ();
				} else {
					NotifyError (ex);
				}
			}
		}

		void NotifyError (Exception exception)
		{
			tracer.Error (exception, Resources.Tracer_ServerPacketListener_Error, clientId ?? "N/A");

			RemoveClient ();

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

		void RemoveClient ()
		{
			if (!string.IsNullOrEmpty (clientId)) {
				connectionProvider.RemoveConnection (clientId);
			}
		}
	}
}
