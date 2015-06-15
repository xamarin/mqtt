using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Flows;
using System.Net.Mqtt.Packets;
using System.Collections.Generic;
using System.Linq;

namespace System.Net.Mqtt.Server
{
	internal class ServerPacketListener : IPacketListener
	{
		static readonly ITracer tracer = Tracer.Get<ServerPacketListener> ();

		readonly IChannel<IPacket> channel;
		readonly IConnectionProvider connectionProvider;
		readonly IProtocolFlowProvider flowProvider;
		readonly ProtocolConfiguration configuration;
		readonly ReplaySubject<IPacket> packets;
		readonly TaskRunner flowRunner;
		CompositeDisposable disposable;
		bool disposed;
		string clientId = string.Empty;
		int keepAlive = 0;

		public ServerPacketListener (IChannel<IPacket> channel,
			IConnectionProvider connectionProvider, 
			IProtocolFlowProvider flowProvider,
			ProtocolConfiguration configuration)
		{
			this.channel = channel;
			this.connectionProvider = connectionProvider;
			this.flowProvider = flowProvider;
			this.configuration = configuration;
			this.packets = new ReplaySubject<IPacket> (window: TimeSpan.FromSeconds(configuration.WaitingTimeoutSecs));
			this.flowRunner = TaskRunner.Get ("ServerFlowRunner");
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
				this.ListenSentPackets ());
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
				this.packets.OnCompleted ();
				this.disposed = true;
			}
		}

		private IDisposable ListenFirstPacket()
		{
			var packetDueTime = TimeSpan.FromSeconds(this.configuration.WaitingTimeoutSecs);

			return this.channel.Receiver
				.FirstOrDefaultAsync ()
				.Timeout (packetDueTime)
				.Subscribe(async packet => {
					if (packet == default (IPacket)) {
						return;
					}

					var connect = packet as Connect;

					if (connect == null) {
						this.NotifyError (Properties.Resources.ServerPacketListener_FirstPacketMustBeConnect);
						return;
					}

					this.clientId = connect.ClientId;
					this.keepAlive = connect.KeepAlive;
					this.connectionProvider.AddConnection (this.clientId, this.channel);

					tracer.Info (Properties.Resources.Tracer_ServerPacketListener_ConnectPacketReceived, this.clientId);

					await this.DispatchPacketAsync (connect)
						.ConfigureAwait(continueOnCapturedContext: false);
				}, async ex => {
					await this.HandleConnectionExceptionAsync (ex)
						.ConfigureAwait(continueOnCapturedContext: false);
				});
		}

		private IDisposable ListenNextPackets()
		{
			return this.channel.Receiver
				.Skip (1)
				.Subscribe (async packet => {
					if (packet is Connect) {
						this.NotifyError (Properties.Resources.ServerPacketListener_SecondConnectNotAllowed);
						return;
					}

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
				}, async () => {
					tracer.Warn (Properties.Resources.Tracer_PacketChannelCompleted, this.clientId);

					if (!string.IsNullOrEmpty (this.clientId)) {
						this.RemoveClient ();

						var publishFlow = this.flowProvider.GetFlow<ServerPublishReceiverFlow> ();

						await publishFlow.SendWillAsync (this.clientId)
							.ConfigureAwait(continueOnCapturedContext: false);
					}
				
					this.packets.OnCompleted ();	
				});
		}

		private IDisposable ListenSentPackets()
		{
			return this.channel.Sender
				.OfType<ConnectAck> ()
				.FirstAsync ()
				.Subscribe (connectAck => {
					if (keepAlive > 0) {
						this.MonitorKeepAliveAsync ();
					}
				});
		}

		private async Task HandleConnectionExceptionAsync(Exception exception)
		{
			if (exception is TimeoutException) {
				this.NotifyError (Properties.Resources.ServerPacketListener_NoConnectReceived, exception);
			} else if (exception is ProtocolConnectionException) {
				tracer.Error (exception, Properties.Resources.Tracer_ServerPacketListener_ConnectionError, this.clientId ?? "N/A");

				var connectEx = exception as ProtocolConnectionException;
				var errorAck = new ConnectAck (connectEx.ReturnCode, existingSession: false);

				try {
					await this.channel.SendAsync (errorAck)
						.ConfigureAwait(continueOnCapturedContext: false);
				} catch (Exception ex) {
					this.NotifyError (ex);
				}
			} else {
				this.NotifyError (exception);
			}
		}

		private void MonitorKeepAliveAsync()
		{
			var tolerance = this.GetKeepAliveTolerance ();

			var keepAliveSubscription = this.channel.Receiver
				.Timeout (tolerance)
				.ObserveOn(NewThreadScheduler.Default)
				.Subscribe (_ => { }, ex => {
					var timeEx = ex as TimeoutException;

					if (timeEx == null) {
						this.NotifyError (ex);
					} else {
						var message = string.Format (Properties.Resources.ServerPacketListener_KeepAliveTimeExceeded, tolerance, this.clientId);

						this.NotifyError(message, timeEx);
					}
				});

			this.disposable.Add (keepAliveSubscription);
		}
		
		private TimeSpan GetKeepAliveTolerance()
		{
			var tolerance = (int)Math.Round (this.keepAlive * 1.5, MidpointRounding.AwayFromZero);

			return TimeSpan.FromSeconds (tolerance);
		}

		private async Task DispatchPacketAsync(IPacket packet)
		{
			var flow = this.flowProvider.GetFlow (packet.Type);

			if (flow == null) {
				return;
			}
			
			try {
				this.packets.OnNext (packet);

				await this.flowRunner.Run (async () => {
					if (packet.Type == PacketType.Publish) {
							var publish = packet as Publish;

							tracer.Info (Properties.Resources.Tracer_ServerPacketListener_DispatchingPublish, flow.GetType().Name, clientId, publish.Topic);
						} else if (packet.Type == PacketType.Subscribe) {
							var subscribe = packet as Subscribe;
							var topics = subscribe.Subscriptions == null ? new List<string> () : subscribe.Subscriptions.Select (s => s.TopicFilter);

							tracer.Info (Properties.Resources.Tracer_ServerPacketListener_DispatchingSubscribe, flow.GetType().Name, clientId, string.Join(", ", topics));
						} else {
							tracer.Info (Properties.Resources.Tracer_ServerPacketListener_DispatchingMessage, packet.Type, flow.GetType().Name, clientId);
						}

					await flow.ExecuteAsync (this.clientId, packet, this.channel)
						.ConfigureAwait(continueOnCapturedContext: false);
				})
				.ConfigureAwait(continueOnCapturedContext: false);
			} catch (Exception ex) {
				if (flow is ServerConnectFlow) {
					this.HandleConnectionExceptionAsync (ex).Wait ();
				} else {
					this.NotifyError (ex);
				}
			}
		}

		private void NotifyError(Exception exception)
		{
			tracer.Error (exception, Properties.Resources.Tracer_ServerPacketListener_Error, this.clientId ?? "N/A");
			
			this.RemoveClient ();
			
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

		private void RemoveClient()
		{
			if (!string.IsNullOrEmpty (this.clientId)) {
				this.connectionProvider.RemoveConnection (this.clientId);
			}
		}
	}
}
