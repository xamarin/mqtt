using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Hermes.Diagnostics;
using Hermes.Flows;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes
{
    public class Client : IClient, IDisposable
    {
		static readonly ITracer tracer = Tracer.Get<Client> ();

		bool protocolDisconnected;
		bool disposed;
		bool isConnected;
		IDisposable packetsSubscription;

		readonly ReplaySubject<ApplicationMessage> receiver;
		readonly ReplaySubject<IPacket> sender;
		readonly IChannel<IPacket> packetChannel;
		readonly IPacketListener packetListener;
		readonly IProtocolFlowProvider flowProvider;
		readonly IRepository<ClientSession> sessionRepository;
		readonly IPacketIdProvider packetIdProvider;
		readonly ProtocolConfiguration configuration;
		readonly TaskRunner packetSender;

        public Client(IChannel<byte[]> binaryChannel, 
			IPacketChannelFactory channelFactory, 
			IPacketListener packetListener,
			IProtocolFlowProvider flowProvider,
			IRepositoryProvider repositoryProvider,
			IPacketIdProvider packetIdProvider,
			ProtocolConfiguration configuration)
        {
			this.receiver = new ReplaySubject<ApplicationMessage> (window: TimeSpan.FromSeconds(configuration.WaitingTimeoutSecs));
			this.sender = new ReplaySubject<IPacket> (window: TimeSpan.FromSeconds(configuration.WaitingTimeoutSecs));

			this.packetListener = packetListener;
			this.flowProvider = flowProvider;
			this.sessionRepository = repositoryProvider.GetRepository<ClientSession>();
			this.packetIdProvider = packetIdProvider;
			this.configuration = configuration;
			this.packetSender = TaskRunner.Get();

			this.packetChannel = channelFactory.Create (binaryChannel);
			this.packetListener.Listen (this.packetChannel);
		}

		public event EventHandler<ClosedEventArgs> Closed = (sender, args) => { };

		public string Id { get; private set; }

		public bool IsConnected
		{
			get
			{
				this.CheckUnderlyingConnection ();

				return this.isConnected && this.packetChannel.IsConnected;
			}
			private set
			{
				this.isConnected = value;
			}
		}

		public IObservable<ApplicationMessage> Receiver { get { return this.receiver; } }

		public IObservable<IPacket> Sender { get { return this.sender; } }

		/// <exception cref="ClientException">ClientException</exception>
		public async Task ConnectAsync (ClientCredentials credentials, Will will = null, bool cleanSession = false)
		{
			if (this.disposed) {
				throw new ObjectDisposedException (this.GetType ().FullName);
			}

			var ack = default (ConnectAck);

			try {
				this.OpenClientSession (credentials.ClientId, cleanSession);

				var connect = new Connect (credentials.ClientId, cleanSession) {
					UserName = credentials.UserName,
					Password = credentials.Password,
					Will = will,
					KeepAlive = this.configuration.KeepAliveSecs
				};

				var connectTimeout = TimeSpan.FromSeconds (this.configuration.WaitingTimeoutSecs);

				await this.SendPacketAsync (connect)
					.ConfigureAwait(continueOnCapturedContext: false);

				ack = await this.packetListener.Packets
					.ObserveOn (NewThreadScheduler.Default)
					.OfType<ConnectAck> ()
					.FirstOrDefaultAsync ()
					.Timeout (connectTimeout);

				if (ack == null) {
					var message = string.Format(Resources.Client_ConnectionDisconnected, credentials.ClientId);

					tracer.Error (message);

					throw new ClientException (message);
				}

				this.Id = credentials.ClientId;
				this.IsConnected = true;
				this.ObservePackets ();
			} catch(TimeoutException timeEx) {
				this.Close (timeEx);
				throw new ClientException (string.Format(Resources.Client_ConnectionTimeout, credentials.ClientId), timeEx);
			} catch(ClientException clientEx) {
				this.Close (clientEx);
				throw;
			} catch (Exception ex) {
				this.Close (ex);
				throw new ClientException (string.Format(Resources.Client_ConnectionError, credentials.ClientId), ex);
			}
		}

		/// <exception cref="ClientException">ClientException</exception>
		public async Task SubscribeAsync (string topicFilter, QualityOfService qos)
		{
			if (this.disposed) {
				throw new ObjectDisposedException (this.GetType ().FullName);
			}

			try {
				var packetId = this.packetIdProvider.GetPacketId ();
				var subscribe = new Subscribe (packetId, new Subscription (topicFilter, qos));

				var ack = default (SubscribeAck);
				var subscribeTimeout = TimeSpan.FromSeconds(this.configuration.WaitingTimeoutSecs);

				await this.SendPacketAsync (subscribe)
					.ConfigureAwait(continueOnCapturedContext: false);

				ack = await this.packetListener.Packets
					.ObserveOn (NewThreadScheduler.Default)
					.OfType<SubscribeAck> ()
					.FirstOrDefaultAsync (x => x.PacketId == packetId)
					.Timeout (subscribeTimeout);

				if (ack == null) {
					var message = string.Format(Resources.Client_SubscriptionDisconnected, this.Id, topicFilter);

					tracer.Error (message);

					throw new ClientException (message);
				}
			} catch(TimeoutException timeEx) {
				this.Close (timeEx);

				var message = string.Format (Resources.Client_SubscribeTimeout, this.Id, topicFilter);

				throw new ClientException (message, timeEx);
			} catch(ClientException clientEx) {
				this.Close (clientEx);
				throw;
			} catch (Exception ex) {
				this.Close (ex);

				var message = string.Format (Resources.Client_SubscribeError, this.Id, topicFilter);

				throw new ClientException (message, ex);
			}
		}

		public async Task PublishAsync (ApplicationMessage message, QualityOfService qos, bool retain = false)
		{
			if (this.disposed) {
				throw new ObjectDisposedException (this.GetType ().FullName);
			}

			try {
				ushort? packetId = qos == QualityOfService.AtMostOnce ? null : (ushort?)this.packetIdProvider.GetPacketId ();
				var publish = new Publish (message.Topic, qos, retain, duplicated: false, packetId: packetId)
				{
					Payload = message.Payload
				};

				var senderFlow = this.flowProvider.GetFlow<PublishSenderFlow> ();

				await senderFlow.SendPublishAsync (this.Id, publish, this.packetChannel)
					.ConfigureAwait(continueOnCapturedContext: false);
			} catch (Exception ex) {
				this.Close (ex);
				throw;
			}
		}

		public async Task UnsubscribeAsync (params string[] topics)
		{
			if (this.disposed) {
				throw new ObjectDisposedException (this.GetType ().FullName);
			}

			try {
				var packetId = this.packetIdProvider.GetPacketId ();
				var unsubscribe = new Unsubscribe(packetId, topics);

				var ack = default (UnsubscribeAck);
				var unsubscribeTimeout = TimeSpan.FromSeconds(this.configuration.WaitingTimeoutSecs);

				await this.SendPacketAsync (unsubscribe)
					.ConfigureAwait(continueOnCapturedContext: false);

				ack = await this.packetListener.Packets
					.ObserveOn (NewThreadScheduler.Default)
					.OfType<UnsubscribeAck> ()
					.FirstOrDefaultAsync (x => x.PacketId == packetId)
					.Timeout (unsubscribeTimeout);

				if (ack == null) {
					var message = string.Format(Resources.Client_UnsubscribeDisconnected, this.Id, string.Join(", ", topics));

					tracer.Error (message);

					throw new ClientException (message);
				}
			} catch(TimeoutException timeEx) {
				this.Close (timeEx);

				var message = string.Format (Resources.Client_UnsubscribeTimeout, this.Id, string.Join(", ", topics));

				tracer.Error (message);

				throw new ClientException (message, timeEx);
			} catch(ClientException clientEx) {
				this.Close (clientEx);
				throw;
			} catch (Exception ex) {
				this.Close (ex);

				var message = string.Format (Resources.Client_UnsubscribeError, this.Id, string.Join(", ", topics));

				tracer.Error (message);

				throw new ClientException (message, ex);
			}
		}

		public async Task DisconnectAsync ()
		{
			if (this.disposed) {
				throw new ObjectDisposedException (this.GetType ().FullName);
			}

			try {
				this.CloseClientSession ();

				await this.SendPacketAsync (new Disconnect ())
					.ConfigureAwait(continueOnCapturedContext: false);

				this.protocolDisconnected = true;
			} catch (Exception ex) {
				this.Close (ex);
				throw;
			}
		}

		public void Close ()
		{
			this.Close (ClosedReason.Disposed);
		}

		void IDisposable.Dispose ()
		{
			this.Close ();
		}

		protected virtual void Dispose (bool disposing)
		{
			if (this.disposed) return;

			if (disposing) {
				tracer.Info (Resources.Tracer_Client_Disposing, this.Id);

				this.receiver.OnCompleted ();

				if (this.packetsSubscription != null) {
					this.packetsSubscription.Dispose ();
				}
				
				this.packetListener.Dispose ();
				this.packetChannel.Dispose ();
				this.IsConnected = false; 
				this.Id = null;
				this.disposed = true;
			}
		}

		private void Close(Exception ex)
		{
			tracer.Error (ex);
			this.receiver.OnError (ex);
			this.Close (ClosedReason.Error, ex.Message);
		}

		private void Close (ClosedReason reason, string message = null)
		{
			this.Dispose (true);
			this.Closed (this, new ClosedEventArgs(reason, message));
			GC.SuppressFinalize (this);
		}

		private void OpenClientSession(string clientId, bool cleanSession)
		{
			var session = this.sessionRepository.Get (s => s.ClientId == clientId);
			var sessionPresent = cleanSession ? false : session != null;

			if (cleanSession && session != null) {
				this.sessionRepository.Delete(session);
				session = null;

				tracer.Info (Resources.Tracer_Client_CleanedOldSession, clientId);
			}

			if (session == null) {
				session = new ClientSession { ClientId = clientId, Clean = cleanSession };

				this.sessionRepository.Create (session);

				tracer.Info (Resources.Tracer_Client_CreatedSession, clientId);
			}
		}

		private void CloseClientSession()
		{
			var session = this.sessionRepository.Get (s => s.ClientId == this.Id);

			if (session == null) {
				var message = string.Format (Resources.SessionRepository_ClientSessionNotFound, this.Id);

				tracer.Error (message);

				throw new ClientException (message);
			}

			if (session.Clean) {
				this.sessionRepository.Delete (session);

				tracer.Info (Resources.Tracer_Client_DeletedSessionOnDisconnect, this.Id);
			}
		}

		private async Task SendPacketAsync(IPacket packet)
		{
			this.sender.OnNext (packet);

			await this.packetSender.Run(() => this.packetChannel.SendAsync (packet))
				.ConfigureAwait(continueOnCapturedContext: false);
		}

		private void CheckUnderlyingConnection ()
		{
			if (this.isConnected && !this.packetChannel.IsConnected) {
				this.Close (ClosedReason.Error, Resources.Client_UnexpectedChannelDisconnection);
			}
		}

		private void ObservePackets ()
		{
			this.packetsSubscription = this.packetListener.Packets
				.ObserveOn(NewThreadScheduler.Default)
				.Subscribe (packet => { 
					if (packet.Type == PacketType.Publish) {
						var publish = packet as Publish;
						var message = new ApplicationMessage (publish.Topic, publish.Payload);

						this.receiver.OnNext (message);

						tracer.Info (Resources.Tracer_NewApplicationMessageReceived, this.Id, publish.Topic);
					}
				}, ex => {
					this.Close (ex);
				}, () => {
					tracer.Warn (Resources.Tracer_Client_PacketsObservableCompleted);

					var reason = this.protocolDisconnected ? ClosedReason.Disposed : ClosedReason.Disconnected;

					this.Close (reason);
				});
		}
	}
}
