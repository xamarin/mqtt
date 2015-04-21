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

		bool disposed;
		bool isConnected;

		readonly IDisposable publishSubscription;
		readonly IDisposable packetsSubscription;
		readonly ReplaySubject<ApplicationMessage> receiver;
		readonly ReplaySubject<IPacket> sender;
		readonly IChannel<IPacket> packetChannel;
		readonly IPacketListener packetListener;
		readonly IProtocolFlowProvider flowProvider;
		readonly IRepository<ClientSession> sessionRepository;
		readonly IRepository<PacketIdentifier> packetIdentifierRepository;
		readonly ProtocolConfiguration configuration;

        public Client(IChannel<byte[]> binaryChannel, 
			IPacketChannelFactory channelFactory, 
			IPacketListener packetListener,
			IProtocolFlowProvider flowProvider,
			IRepositoryProvider repositoryProvider,
			ProtocolConfiguration configuration)
        {
			this.receiver = new ReplaySubject<ApplicationMessage> (window: TimeSpan.FromSeconds(configuration.WaitingTimeoutSecs));
			this.sender = new ReplaySubject<IPacket> (window: TimeSpan.FromSeconds(configuration.WaitingTimeoutSecs));

			this.packetListener = packetListener;
			this.flowProvider = flowProvider;
			this.sessionRepository = repositoryProvider.GetRepository<ClientSession>();
			this.packetIdentifierRepository = repositoryProvider.GetRepository<PacketIdentifier>();
			this.configuration = configuration;

			this.packetChannel = channelFactory.Create (binaryChannel);
			this.packetListener.Listen (this.packetChannel);

			this.publishSubscription = this.packetListener.Packets
				.OfType<Publish>()
				.SubscribeOn(NewThreadScheduler.Default)
				.Subscribe (publish => {
					tracer.Info (Resources.Tracer_NewApplicationMessageReceived, this.Id, DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff"), publish.Topic);

					var message = new ApplicationMessage (publish.Topic, publish.Payload);

					this.receiver.OnNext (message);
				});

			this.packetsSubscription = this.packetListener.Packets
				.Subscribe (_ => { }, ex => {
					this.receiver.OnError (ex);
					this.sender.OnError (ex);
					this.Close (ex);
				}, () => {
					tracer.Warn (Resources.Tracer_Client_PacketsObservableCompleted, DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff"));

					this.receiver.OnCompleted ();
					this.Close ();
				});
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
		public async Task ConnectAsync (ClientCredentials credentials, bool cleanSession = false)
		{
			await this.ConnectAsync (credentials, null, cleanSession);
		}

		/// <exception cref="ClientException">ClientException</exception>
		public async Task ConnectAsync (ClientCredentials credentials, Will will, bool cleanSession = false)
		{
			if (this.disposed) {
				throw new ObjectDisposedException (this.GetType ().FullName);
			}

			this.OpenClientSession (credentials.ClientId, cleanSession);

			var connect = new Connect (credentials.ClientId, cleanSession) {
				UserName = credentials.UserName,
				Password = credentials.Password,
				Will = will,
				KeepAlive = this.configuration.KeepAliveSecs
			};

			var ack = default (ConnectAck);
			var connectTimeout = TimeSpan.FromSeconds (this.configuration.WaitingTimeoutSecs);

			try {
				await this.SendPacket (connect);

				ack = await this.packetListener.Packets
					.OfType<ConnectAck> ()
					.FirstOrDefaultAsync()
					.Timeout(connectTimeout);
			} catch(TimeoutException timeEx) {
				this.Close (timeEx);
				throw new ClientException (string.Format(Resources.Client_ConnectionTimeout, credentials.ClientId), timeEx);
			} catch (Exception ex) {
				this.Close (ex);
				throw new ClientException (string.Format(Resources.Client_ConnectionError, credentials.ClientId), ex);
			}

			if (ack == null) {
				var message = string.Format(Resources.Client_ConnectionDisconnected, credentials.ClientId);

				tracer.Error (message);

				throw new ClientException (message);
			}

			this.Id = credentials.ClientId;
			this.IsConnected = true;
		}

		/// <exception cref="ClientException">ClientException</exception>
		public async Task SubscribeAsync (string topicFilter, QualityOfService qos)
		{
			if (this.disposed) {
				throw new ObjectDisposedException (this.GetType ().FullName);
			}

			var packetId = this.packetIdentifierRepository.GetPacketIdentifier();
			var subscribe = new Subscribe (packetId, new Subscription (topicFilter, qos));

			var ack = default (SubscribeAck);
			var subscribeTimeout = TimeSpan.FromSeconds(this.configuration.WaitingTimeoutSecs);

			try {
				await this.SendPacket (subscribe);

				ack = await this.packetListener.Packets
					.OfType<SubscribeAck> ()
					.FirstOrDefaultAsync(x => x.PacketId == packetId)
					.Timeout(subscribeTimeout);
			} catch(TimeoutException timeEx) {
				this.Close (timeEx);

				var message = string.Format (Resources.Client_SubscribeTimeout, this.Id, topicFilter);

				throw new ClientException (message, timeEx);
			} catch (Exception ex) {
				this.Close (ex);

				var message = string.Format (Resources.Client_SubscribeError, this.Id, topicFilter);

				throw new ClientException (message, ex);
			}

			if (ack == null) {
				var message = string.Format(Resources.Client_SubscriptionDisconnected, this.Id, topicFilter);

				tracer.Error (message);

				throw new ClientException (message);
			}
		}

		public async Task PublishAsync (ApplicationMessage message, QualityOfService qos, bool retain = false)
		{
			if (this.disposed) {
				throw new ObjectDisposedException (this.GetType ().FullName);
			}

			var packetId = this.packetIdentifierRepository.GetPacketIdentifier(qos);
			var publish = new Publish (message.Topic, qos, retain, duplicated: false, packetId: packetId)
			{
				Payload = message.Payload
			};

			var senderFlow = this.flowProvider.GetFlow<PublishSenderFlow> ();

			try {
				await senderFlow.SendPublishAsync (this.Id, publish, this.packetChannel);
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

			var packetId = this.packetIdentifierRepository.GetPacketIdentifier();
			var unsubscribe = new Unsubscribe(packetId, topics);

			var ack = default (UnsubscribeAck);
			var unsubscribeTimeout = TimeSpan.FromSeconds(this.configuration.WaitingTimeoutSecs);

			try {
				await this.SendPacket (unsubscribe);

				ack = await this.packetListener.Packets
					.OfType<UnsubscribeAck> ()
					.FirstOrDefaultAsync (x => x.PacketId == packetId)
					.Timeout(unsubscribeTimeout);
			} catch(TimeoutException timeEx) {
				this.Close (timeEx);

				var message = string.Format (Resources.Client_UnsubscribeTimeout, this.Id, string.Join(", ", topics));

				tracer.Error (message);

				throw new ClientException (message, timeEx);
			} catch (Exception ex) {
				this.Close (ex);

				var message = string.Format (Resources.Client_UnsubscribeError, this.Id, string.Join(", ", topics));

				tracer.Error (message);

				throw new ClientException (message, ex);
			}

			if (ack == null) {
				var message = string.Format(Resources.Client_UnsubscribeDisconnected, this.Id, string.Join(", ", topics));

				tracer.Error (message);

				throw new ClientException (message);
			}
		}

		public async Task DisconnectAsync ()
		{
			if (this.disposed) {
				throw new ObjectDisposedException (this.GetType ().FullName);
			}

			this.CloseClientSession ();

			var disconnect = new Disconnect ();

			try {
				await this.SendPacket (disconnect).ContinueWith(t => {
					this.Close (ClosedReason.Disconnect);
				});
			} catch (Exception ex) {
				this.Close (ex);
				throw;
			}
		}

		public void Close ()
		{
			this.Close (ClosedReason.Disconnect);
		}

		void IDisposable.Dispose ()
		{
			this.Close (ClosedReason.Dispose);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (this.disposed) return;

			if (disposing) {
				this.publishSubscription.Dispose ();
				this.packetsSubscription.Dispose ();
				this.packetChannel.Dispose ();
				this.IsConnected = false; 
				this.Id = null;
				this.disposed = true;
			}
		}

		private void Close(Exception ex)
		{
			tracer.Error (ex);
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
			}

			if (session == null) {
				session = new ClientSession { ClientId = clientId, Clean = cleanSession };

				this.sessionRepository.Create (session);
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
			}
		}

		private async Task SendPacket(IPacket packet)
		{
			this.sender.OnNext (packet);

			await this.packetChannel.SendAsync (packet);
		}

		private void CheckUnderlyingConnection ()
		{
			if (this.isConnected && !this.packetChannel.IsConnected) {
				this.Close (ClosedReason.Error, Resources.Client_UnexpectedChannelDisconnection);
			}
		}
	}
}
