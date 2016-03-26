using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Flows;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Storage;
using System.Net.Mqtt.Exceptions;

namespace System.Net.Mqtt.Client
{
	public class Client : IClient, IDisposable
	{
		bool protocolDisconnected;
		bool disposed;
		bool isConnected;
		IDisposable packetsSubscription;

		readonly ITracer tracer;
		readonly ReplaySubject<ApplicationMessage> receiver;
		readonly ReplaySubject<IPacket> sender;
		readonly IChannel<IPacket> packetChannel;
		readonly IProtocolFlowProvider flowProvider;
		readonly IRepository<ClientSession> sessionRepository;
		readonly IPacketIdProvider packetIdProvider;
		readonly ProtocolConfiguration configuration;
		readonly TaskRunner clientSender;
		readonly IPacketListener packetListener;

		internal Client (IChannel<IPacket> packetChannel,
			IProtocolFlowProvider flowProvider,
			IRepositoryProvider repositoryProvider,
			IPacketIdProvider packetIdProvider,
			ITracerManager tracerManager,
			ProtocolConfiguration configuration)
		{
			tracer = tracerManager.Get<Client> ();

			receiver = new ReplaySubject<ApplicationMessage> (window: TimeSpan.FromSeconds (configuration.WaitingTimeoutSecs));
			sender = new ReplaySubject<IPacket> (window: TimeSpan.FromSeconds (configuration.WaitingTimeoutSecs));

			this.packetChannel = packetChannel;
			this.flowProvider = flowProvider;
			sessionRepository = repositoryProvider.GetRepository<ClientSession> ();
			this.packetIdProvider = packetIdProvider;
			this.configuration = configuration;
			clientSender = TaskRunner.Get ();
			packetListener = new ClientPacketListener (packetChannel, flowProvider, tracerManager, configuration);

			packetListener.Listen ();
		}

		public event EventHandler<ClosedEventArgs> Closed = (sender, args) => { };

		public string Id { get; private set; }

		public bool IsConnected
		{
			get
			{
				CheckUnderlyingConnection ();

				return isConnected && packetChannel.IsConnected;
			}
			private set
			{
				isConnected = value;
			}
		}

		public IObservable<ApplicationMessage> Receiver { get { return receiver; } }

		internal IObservable<IPacket> Sender { get { return sender; } }

		/// <exception cref="ClientException">ClientException</exception>
		public async Task ConnectAsync (ClientCredentials credentials, Will will = null, bool cleanSession = false)
		{
			if (disposed) {
				throw new ObjectDisposedException (GetType ().FullName);
			}

			var ack = default (ConnectAck);

			try {
				OpenClientSession (credentials.ClientId, cleanSession);

				var connect = new Connect (credentials.ClientId, cleanSession) {
					UserName = credentials.UserName,
					Password = credentials.Password,
					Will = will,
					KeepAlive = configuration.KeepAliveSecs
				};

				var connectTimeout = TimeSpan.FromSeconds (configuration.WaitingTimeoutSecs);

				await SendPacketAsync (connect)
					.ConfigureAwait (continueOnCapturedContext: false);

				ack = await packetListener.Packets
					.ObserveOn (NewThreadScheduler.Default)
					.OfType<ConnectAck> ()
					.FirstOrDefaultAsync ()
					.Timeout (connectTimeout);

				if (ack == null) {
					var message = string.Format(Properties.Resources.Client_ConnectionDisconnected, credentials.ClientId);

					throw new ClientException (message);
				}

				if (ack.Status != ConnectionStatus.Accepted) {
					throw new MqttConnectionException (ack.Status);
				}

				Id = credentials.ClientId;
				IsConnected = true;
				ObservePackets ();
			} catch (TimeoutException timeEx) {
				Close (timeEx);
				throw new ClientException (string.Format (Properties.Resources.Client_ConnectionTimeout, credentials.ClientId), timeEx);
			} catch (MqttConnectionException connectionEx) {
				Close (connectionEx);

				var message = string.Format(Properties.Resources.Client_ConnectNotAccepted, credentials.ClientId, connectionEx.ReturnCode);

				throw new ClientException (message, connectionEx);
			} catch (ClientException clientEx) {
				Close (clientEx);
				throw;
			} catch (Exception ex) {
				Close (ex);
				throw new ClientException (string.Format (Properties.Resources.Client_ConnectionError, credentials.ClientId), ex);
			}
		}

		/// <exception cref="ClientException">ClientException</exception>
		public async Task SubscribeAsync (string topicFilter, QualityOfService qos)
		{
			if (disposed) {
				throw new ObjectDisposedException (GetType ().FullName);
			}

			try {
				var packetId = packetIdProvider.GetPacketId ();
				var subscribe = new Subscribe (packetId, new Subscription (topicFilter, qos));

				var ack = default (SubscribeAck);
				var subscribeTimeout = TimeSpan.FromSeconds(configuration.WaitingTimeoutSecs);

				await SendPacketAsync (subscribe)
					.ConfigureAwait (continueOnCapturedContext: false);

				ack = await packetListener.Packets
					.ObserveOn (NewThreadScheduler.Default)
					.OfType<SubscribeAck> ()
					.FirstOrDefaultAsync (x => x.PacketId == packetId)
					.Timeout (subscribeTimeout);

				if (ack == null) {
					var message = string.Format(Properties.Resources.Client_SubscriptionDisconnected, Id, topicFilter);

					tracer.Error (message);

					throw new ClientException (message);
				}
			} catch (TimeoutException timeEx) {
				Close (timeEx);

				var message = string.Format (Properties.Resources.Client_SubscribeTimeout, Id, topicFilter);

				throw new ClientException (message, timeEx);
			} catch (ClientException clientEx) {
				Close (clientEx);
				throw;
			} catch (Exception ex) {
				Close (ex);

				var message = string.Format (Properties.Resources.Client_SubscribeError, Id, topicFilter);

				throw new ClientException (message, ex);
			}
		}

		public async Task PublishAsync (ApplicationMessage message, QualityOfService qos, bool retain = false)
		{
			if (disposed) {
				throw new ObjectDisposedException (GetType ().FullName);
			}

			try {
				ushort? packetId = qos == QualityOfService.AtMostOnce ? null : (ushort?)packetIdProvider.GetPacketId ();
				var publish = new Publish (message.Topic, qos, retain, duplicated: false, packetId: packetId)
				{
					Payload = message.Payload
				};

				var senderFlow = flowProvider.GetFlow<PublishSenderFlow> ();

				await clientSender.Run (async () => {
					await senderFlow.SendPublishAsync (Id, publish, packetChannel)
						.ConfigureAwait (continueOnCapturedContext: false);
				}).ConfigureAwait (continueOnCapturedContext: false);
			} catch (Exception ex) {
				Close (ex);
				throw;
			}
		}

		public async Task UnsubscribeAsync (params string[] topics)
		{
			if (disposed) {
				throw new ObjectDisposedException (GetType ().FullName);
			}

			try {
				var packetId = packetIdProvider.GetPacketId ();
				var unsubscribe = new Unsubscribe(packetId, topics);

				var ack = default (UnsubscribeAck);
				var unsubscribeTimeout = TimeSpan.FromSeconds(configuration.WaitingTimeoutSecs);

				await SendPacketAsync (unsubscribe)
					.ConfigureAwait (continueOnCapturedContext: false);

				ack = await packetListener.Packets
					.ObserveOn (NewThreadScheduler.Default)
					.OfType<UnsubscribeAck> ()
					.FirstOrDefaultAsync (x => x.PacketId == packetId)
					.Timeout (unsubscribeTimeout);

				if (ack == null) {
					var message = string.Format(Properties.Resources.Client_UnsubscribeDisconnected, Id, string.Join(", ", topics));

					tracer.Error (message);

					throw new ClientException (message);
				}
			} catch (TimeoutException timeEx) {
				Close (timeEx);

				var message = string.Format (Properties.Resources.Client_UnsubscribeTimeout, Id, string.Join(", ", topics));

				tracer.Error (message);

				throw new ClientException (message, timeEx);
			} catch (ClientException clientEx) {
				Close (clientEx);
				throw;
			} catch (Exception ex) {
				Close (ex);

				var message = string.Format (Properties.Resources.Client_UnsubscribeError, Id, string.Join(", ", topics));

				tracer.Error (message);

				throw new ClientException (message, ex);
			}
		}

		public async Task DisconnectAsync ()
		{
			if (disposed) {
				throw new ObjectDisposedException (GetType ().FullName);
			}

			try {
				CloseClientSession ();

				await SendPacketAsync (new Disconnect ())
					.ConfigureAwait (continueOnCapturedContext: false);

				protocolDisconnected = true;
			} catch (Exception ex) {
				Close (ex);
				throw;
			}
		}

		public void Close ()
		{
			Close (ClosedReason.Disposed);
		}

		void IDisposable.Dispose ()
		{
			Close ();
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposed) return;

			if (disposing) {
				receiver.OnCompleted ();

				if (packetsSubscription != null) {
					packetsSubscription.Dispose ();
				}

				packetListener.Dispose ();
				packetChannel.Dispose ();
				(clientSender as IDisposable)?.Dispose ();
				IsConnected = false;
				Id = null;
				disposed = true;
			}
		}

		void Close (Exception ex)
		{
			tracer.Error (ex);
			receiver.OnError (ex);
			Close (ClosedReason.Error, ex.Message);
		}

		void Close (ClosedReason reason, string message = null)
		{
			tracer.Info (Properties.Resources.Tracer_Client_Disposing, Id, reason);
			Dispose (true);
			Closed (this, new ClosedEventArgs (reason, message));
			GC.SuppressFinalize (this);
		}

		void OpenClientSession (string clientId, bool cleanSession)
		{
			var session = sessionRepository.Get (s => s.ClientId == clientId);
			var sessionPresent = cleanSession ? false : session != null;

			if (cleanSession && session != null) {
				sessionRepository.Delete (session);
				session = null;

				tracer.Info (Properties.Resources.Tracer_Client_CleanedOldSession, clientId);
			}

			if (session == null) {
				session = new ClientSession { ClientId = clientId, Clean = cleanSession };

				sessionRepository.Create (session);

				tracer.Info (Properties.Resources.Tracer_Client_CreatedSession, clientId);
			}
		}

		void CloseClientSession ()
		{
			var session = sessionRepository.Get (s => s.ClientId == Id);

			if (session == null) {
				var message = string.Format (Properties.Resources.SessionRepository_ClientSessionNotFound, Id);

				tracer.Error (message);

				throw new ClientException (message);
			}

			if (session.Clean) {
				sessionRepository.Delete (session);

				tracer.Info (Properties.Resources.Tracer_Client_DeletedSessionOnDisconnect, Id);
			}
		}

		async Task SendPacketAsync (IPacket packet)
		{
			sender.OnNext (packet);

			await clientSender.Run (async () => await packetChannel.SendAsync (packet).ConfigureAwait (continueOnCapturedContext: false))
				.ConfigureAwait (continueOnCapturedContext: false);
		}

		void CheckUnderlyingConnection ()
		{
			if (isConnected && !packetChannel.IsConnected) {
				Close (ClosedReason.Error, Properties.Resources.Client_UnexpectedChannelDisconnection);
			}
		}

		void ObservePackets ()
		{
			packetsSubscription = packetListener.Packets
				.ObserveOn (NewThreadScheduler.Default)
				.Subscribe (packet => {
					if (packet.Type == PacketType.Publish) {
						var publish = packet as Publish;
						var message = new ApplicationMessage (publish.Topic, publish.Payload);

						receiver.OnNext (message);

						tracer.Info (Properties.Resources.Tracer_NewApplicationMessageReceived, Id, publish.Topic);
					}
				}, ex => {
					Close (ex);
				}, () => {
					tracer.Warn (Properties.Resources.Tracer_Client_PacketsObservableCompleted);

					var reason = protocolDisconnected ? ClosedReason.Disposed : ClosedReason.Disconnected;

					Close (reason);
				});
		}
	}
}
