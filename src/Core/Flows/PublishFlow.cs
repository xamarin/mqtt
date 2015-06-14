using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Storage;
using System.Reactive.Concurrency;
using System.Threading;

namespace System.Net.Mqtt.Flows
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
				throw new ProtocolException (string.Format(Properties.Resources.SessionRepository_ClientSessionNotFound, clientId));
			}

			var pendingAcknowledgement = session
				.GetPendingAcknowledgements()
				.FirstOrDefault(u => u.Type == type && u.PacketId == packetId);

			session.RemovePendingAcknowledgement (pendingAcknowledgement);

			this.sessionRepository.Update (session);
		}

		protected Task MonitorAckAsync<T>(IFlowPacket sentMessage, string clientId, IChannel<IPacket> channel)
			where T : IFlowPacket
		{
			return Task.Run (() => {
				var retries = 0;
				var ackSignal = new ManualResetEventSlim (initialState: false);

				var intervalSubscription = Observable
					.Interval (TimeSpan.FromSeconds (this.configuration.WaitingTimeoutSecs), NewThreadScheduler.Default)
					.Subscribe (_ => {
						if (!ackSignal.IsSet) {
							tracer.Warn (Properties.Resources.Tracer_PublishFlow_RetryingQoSFlow, sentMessage.Type, clientId);

							if (channel.IsConnected) {
								channel.SendAsync (sentMessage);
							} else {
								ackSignal.Set ();
							}

							retries++;
						}
					});

				var ackSubscription = channel.Receiver
					.ObserveOn(NewThreadScheduler.Default)
					.OfType<T> ()
					.Where (x => x.PacketId == sentMessage.PacketId)
					.Subscribe (x => {
						ackSignal.Set();
					});

				ackSignal.Wait();
				intervalSubscription.Dispose();
				ackSubscription.Dispose ();
			});
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
				throw new ProtocolException (string.Format (Properties.Resources.SessionRepository_ClientSessionNotFound, clientId));
			}

			session.AddPendingAcknowledgement (unacknowledgeMessage);

			this.sessionRepository.Update (session);
		}
	}
}
