using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Timers;
using Hermes.Diagnostics;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Threading.Tasks;

namespace Hermes.Flows
{
	public abstract class PublishFlow : IPublishFlow
	{
		private static readonly ITracer tracer = Tracer.Get<PublishFlow> ();

		protected readonly IRepository<ClientSession> sessionRepository;
		protected readonly ProtocolConfiguration configuration;

		protected PublishFlow (IRepository<ClientSession> sessionRepository, 
			ProtocolConfiguration configuration)
		{
			this.sessionRepository = sessionRepository;
			this.configuration = configuration;
		}

		public abstract Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel);

		public async Task SendAckAsync (string clientId, IFlowPacket ack, IChannel<IPacket> channel, PendingMessageStatus status = PendingMessageStatus.PendingToSend)
		{
			if((ack.Type == PacketType.PublishReceived || ack.Type == PacketType.PublishRelease) &&
				status == PendingMessageStatus.PendingToSend) {
				this.SavePendingAcknowledgement (ack, clientId);
			}

			if (!channel.IsConnected) {
				return;
			}

			await channel.SendAsync (ack)
				.ConfigureAwait(continueOnCapturedContext: false);

			if(ack.Type == PacketType.PublishReceived) {
				await this.MonitorAckAsync<PublishRelease> (ack, clientId, channel)
					.ConfigureAwait(continueOnCapturedContext: false);
			} else if (ack.Type == PacketType.PublishRelease) {
				await this.MonitorAckAsync<PublishComplete> (ack, clientId, channel)
					.ConfigureAwait(continueOnCapturedContext: false);
			}
		}

		protected void RemovePendingAcknowledgement(string clientId, ushort packetId, PacketType type)
		{
			var session = this.sessionRepository.Get (s => s.ClientId == clientId);

			if (session == null) {
				throw new ProtocolException (string.Format(Resources.SessionRepository_ClientSessionNotFound, clientId));
			}

			var pendingAcknowledgement = session
				.GetPendingAcknowledgements()
				.FirstOrDefault(u => u.Type == type && u.PacketId == packetId);

			session.RemovePendingAcknowledgement (pendingAcknowledgement);

			this.sessionRepository.Update (session);
		}

		protected async Task MonitorAckAsync<T>(IFlowPacket sentMessage, string clientId, IChannel<IPacket> channel)
			where T : IFlowPacket
		{
			var ackSubject = new Subject<T> ();
			var retries = 0;
			var qosTimer = new Timer();

			qosTimer.AutoReset = true;
			qosTimer.Interval = this.configuration.WaitingTimeoutSecs * 1000;
			qosTimer.Elapsed += async (sender, e) => {
				if (retries == this.configuration.QualityOfServiceAckRetries) {
					ackSubject.OnError (new ProtocolException (string.Format (Resources.PublishFlow_AckMonitor_ExceededMaximumAckRetries, this.configuration.QualityOfServiceAckRetries)));
				}

				tracer.Warn (Resources.Tracer_PublishFlow_RetryingQoSFlow, sentMessage.Type, clientId);

				try {
					if (channel.IsConnected) {
						await channel.SendAsync (sentMessage)
							.ConfigureAwait(continueOnCapturedContext: false);
					} else {
						ackSubject.OnCompleted ();
					}	
				} catch (Exception ex) {
					ackSubject.OnError (ex);
				}

				retries++;
			};
			qosTimer.Start ();

			var ackSubscription = channel.Receiver
				.ObserveOn(NewThreadScheduler.Default)
				.OfType<T> ()
				.Where (x => x.PacketId == sentMessage.PacketId)
				.Subscribe (x => {
					ackSubject.OnNext (x);
				});

			try {
				await ackSubject.FirstOrDefaultAsync ()
					.ToTask().ConfigureAwait(continueOnCapturedContext: false);
			} finally {
				ackSubscription.Dispose ();
				ackSubject.Dispose ();
				qosTimer.Dispose ();
			}
		}

		private void SavePendingAcknowledgement(IFlowPacket ack, string clientId)
		{
			if (ack.Type != PacketType.PublishReceived && ack.Type != PacketType.PublishRelease) {
				return;
			}

			var unacknowledgeMessage = new PendingAcknowledgement {
				PacketId = ack.PacketId,
				Type = ack.Type
			};
			
			var session = this.sessionRepository.Get (s => s.ClientId == clientId);

			if (session == null) {
				throw new ProtocolException (string.Format (Resources.SessionRepository_ClientSessionNotFound, clientId));
			}

			session.AddPendingAcknowledgement (unacknowledgeMessage);

			this.sessionRepository.Update (session);
		}
	}
}
