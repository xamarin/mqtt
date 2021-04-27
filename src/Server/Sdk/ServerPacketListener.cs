using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mqtt.Sdk.Flows;
using System.Net.Mqtt.Sdk.Packets;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using ServerProperties = System.Net.Mqtt.Server.Properties;

namespace System.Net.Mqtt.Sdk
{
	internal class ServerPacketListener : IPacketListener
	{
		static readonly ITracer tracer = Tracer.Get<ServerPacketListener> ();

		readonly IMqttChannel<IPacket> channel;
		readonly IConnectionProvider connectionProvider;
		readonly IProtocolFlowProvider flowProvider;
		readonly MqttConfiguration configuration;
		readonly ReplaySubject<IPacket> packets;
		CompositeDisposable listenerDisposable;
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
				tracer.Info (Properties.Resources.Mqtt_Disposing, GetType ().FullName);

				listenerDisposable.Dispose ();
				packets.OnCompleted ();
				disposed = true;
			}
		}

		IDisposable ListenFirstPacket ()
		{
			var packetDueTime = TimeSpan.FromSeconds(configuration.WaitTimeoutSecs);

			return channel
                .ReceiverStream
				.FirstOrDefaultAsync ()
				.Timeout (packetDueTime)
				.Subscribe (async packet => {
					if (packet == default (IPacket)) {
						return;
					}

					var connect = packet as Connect;

					if (connect == null) {
						await NotifyErrorAsync (ServerProperties.Resources.ServerPacketListener_FirstPacketMustBeConnect)
                            .ConfigureAwait (continueOnCapturedContext: false);

						return;
					}

					clientId = connect.ClientId;
					keepAlive = connect.KeepAlive;

					await connectionProvider
						.AddConnectionAsync (clientId, channel)
						.ConfigureAwait(continueOnCapturedContext: false);

					tracer.Info (ServerProperties.Resources.ServerPacketListener_ConnectPacketReceived, clientId);

					await DispatchPacketAsync (connect)
						.ConfigureAwait (continueOnCapturedContext: false);
				}, async ex => {
					await HandleConnectionExceptionAsync (ex)
						.ConfigureAwait (continueOnCapturedContext: false);
				});
		}

		IDisposable ListenNextPackets ()
		{
			return channel
                .ReceiverStream
				.Skip (1)
				.Subscribe (async packet => {
					if (packet is Connect) {
						await NotifyErrorAsync (new MqttProtocolViolationException (ServerProperties.Resources.ServerPacketListener_SecondConnectNotAllowed))
                            .ConfigureAwait (continueOnCapturedContext: false);

						return;
					}

					await DispatchPacketAsync (packet)
						.ConfigureAwait (continueOnCapturedContext: false);
				}, async ex => {
					await NotifyErrorAsync (ex).ConfigureAwait (continueOnCapturedContext: false);
				});
		}

		IDisposable ListenCompletionAndErrors ()
		{
			return channel
                .ReceiverStream
                .Subscribe (_ => { },
				    async ex => {
					    await NotifyErrorAsync (ex).ConfigureAwait (continueOnCapturedContext: false);
				    }, async () => {
                        await SendLastWillAsync ().ConfigureAwait (continueOnCapturedContext: false);
                        await CompletePacketStreamAsync ().ConfigureAwait(continueOnCapturedContext: false);
				    }
                );
		}

		IDisposable ListenSentPackets ()
		{
			return channel.SenderStream
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
				await NotifyErrorAsync (ServerProperties.Resources.ServerPacketListener_NoConnectReceived, exception)
                    .ConfigureAwait (continueOnCapturedContext: false);
			} else if (exception is MqttConnectionException) {
				tracer.Error (exception, ServerProperties.Resources.ServerPacketListener_ConnectionError, clientId ?? "N/A");

				var connectEx = exception as MqttConnectionException;
				var errorAck = new ConnectAck (connectEx.ReturnCode, existingSession: false);

				try {
					await channel.SendAsync (errorAck)
						.ConfigureAwait (continueOnCapturedContext: false);
				} catch (Exception ex) {
					await NotifyErrorAsync (ex).ConfigureAwait (continueOnCapturedContext: false);
				}
			} else {
				await NotifyErrorAsync (exception).ConfigureAwait (continueOnCapturedContext: false);
			}
		}

		void MonitorKeepAliveAsync ()
		{
			var tolerance = GetKeepAliveTolerance ();

			var keepAliveSubscription = channel
                .ReceiverStream
				.Timeout (tolerance)
				.Subscribe (_ => { }, async ex => {
					var timeEx = ex as TimeoutException;

					if (timeEx == null) {
						await NotifyErrorAsync (ex).ConfigureAwait (continueOnCapturedContext: false);
					} else {
						var message = string.Format (ServerProperties.Resources.ServerPacketListener_KeepAliveTimeExceeded, tolerance, clientId);

						await NotifyErrorAsync (message, timeEx).ConfigureAwait (continueOnCapturedContext: false);
					}
				});

			listenerDisposable.Add (keepAliveSubscription);
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

				if (packet.Type == MqttPacketType.Publish)
				{
					var publish = packet as Publish;

					tracer.Info(ServerProperties.Resources.ServerPacketListener_DispatchingPublish, flow.GetType().Name, clientId, publish.Topic);
				}
				else if (packet.Type == MqttPacketType.Subscribe)
				{
					var subscribe = packet as Subscribe;
					var topics = subscribe.Subscriptions == null ? new List<string>() : subscribe.Subscriptions.Select(s => s.TopicFilter);

					tracer.Info(ServerProperties.Resources.ServerPacketListener_DispatchingSubscribe, flow.GetType().Name, clientId, string.Join(", ", topics));
				}
				else
				{
					tracer.Info(ServerProperties.Resources.ServerPacketListener_DispatchingMessage, packet.Type, flow.GetType().Name, clientId);
				}

				await flow
					.ExecuteAsync(clientId, packet, channel)
					.ConfigureAwait(continueOnCapturedContext: false);
			} catch (Exception ex) {
				if (flow is ServerConnectFlow) {
					HandleConnectionExceptionAsync (ex).Wait ();
				} else {
					await NotifyErrorAsync (ex).ConfigureAwait (continueOnCapturedContext: false);
				}
			}
		}

		async Task NotifyErrorAsync (Exception exception)
		{
			tracer.Error (exception, ServerProperties.Resources.ServerPacketListener_Error, clientId ?? "N/A");

            listenerDisposable.Dispose ();
			await RemoveClientAsync ().ConfigureAwait(continueOnCapturedContext: false);
            await SendLastWillAsync ().ConfigureAwait (continueOnCapturedContext: false);
            packets.OnError (exception);
            await CompletePacketStreamAsync ().ConfigureAwait(continueOnCapturedContext: false);
		}

		Task NotifyErrorAsync (string message)
		{
			return NotifyErrorAsync (new MqttException (message));
		}

        Task NotifyErrorAsync (string message, Exception exception)
		{
			return NotifyErrorAsync (new MqttException (message, exception));
		}

        async Task SendLastWillAsync ()
        {
            if (string.IsNullOrEmpty (clientId)) {
                return;
            }

            var publishFlow = flowProvider.GetFlow<IServerPublishReceiverFlow> ();

            await publishFlow
                .SendWillAsync (clientId)
                .ConfigureAwait (continueOnCapturedContext: false);
        }

        async Task RemoveClientAsync()
        {
            if (string.IsNullOrEmpty (clientId)) {
                return;
            }

            await connectionProvider
				.RemoveConnectionAsync (clientId)
				.ConfigureAwait(continueOnCapturedContext: false);
        }

        async Task CompletePacketStreamAsync ()
        {
            if (!string.IsNullOrEmpty (clientId)) {
                await RemoveClientAsync ().ConfigureAwait(continueOnCapturedContext: false);
            }

            tracer.Warn (ServerProperties.Resources.PacketChannelCompleted, clientId ?? "N/A");

            packets.OnCompleted ();
        }
    }
}
