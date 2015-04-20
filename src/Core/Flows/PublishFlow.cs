using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Hermes.Diagnostics;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

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

			await channel.SendAsync (ack);

			if(ack.Type == PacketType.PublishReceived) {
				await this.MonitorAckAsync<PublishRelease> (ack, clientId, channel);
			} else if (ack.Type == PacketType.PublishRelease) {
				await this.MonitorAckAsync<PublishComplete> (ack, clientId, channel);
			}
		}

		protected void RemovePendingAcknowledgement(string clientId, ushort packetId, PacketType type)
		{
			var session = this.sessionRepository.Get (s => s.ClientId == clientId);

			if (session == null) {
				throw new ProtocolException (string.Format(Resources.SessionRepository_ClientSessionNotFound, clientId));
			}

			var pendingAcknowledgement = session.PendingAcknowledgements
				.FirstOrDefault(u => u.Type == type && u.PacketId == packetId);

			session.PendingAcknowledgements.Remove (pendingAcknowledgement);

			this.sessionRepository.Update (session);
		}

		protected async Task MonitorAckAsync<T>(IFlowPacket sentMessage, string clientId, IChannel<IPacket> channel)
			where T : IFlowPacket
		{
			await this.GetAckMonitor<T> (sentMessage, clientId, channel).ObserveOn(Scheduler.Default);
		}

		protected IObservable<T> GetAckMonitor<T>(IFlowPacket sentMessage, string clientId, IChannel<IPacket> channel, int retries = 0)
			where T : IFlowPacket
		{
			if (retries == this.configuration.QualityOfServiceAckRetries) {
				throw new ProtocolException (string.Format(Resources.PublishFlow_AckMonitor_ExceededMaximumAckRetries, this.configuration.QualityOfServiceAckRetries));
			}

			return channel.Receiver.OfType<T> ()
				.FirstOrDefaultAsync (x => x.PacketId == sentMessage.PacketId)
				.Timeout (TimeSpan.FromSeconds (this.configuration.WaitingTimeoutSecs))
				.ObserveOn(Scheduler.Default)
				.Catch<T, TimeoutException> (timeEx => {
					tracer.Warn (timeEx, Resources.Tracer_PublishFlow_RetryingQoSFlow, DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff"), sentMessage.Type, clientId);

					channel.SendAsync (sentMessage).Wait ();

					return this.GetAckMonitor<T> (sentMessage, clientId, channel, retries + 1);
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
				throw new ProtocolException (string.Format (Resources.SessionRepository_ClientSessionNotFound, clientId));
			}

			session.PendingAcknowledgements.Add (unacknowledgeMessage);

			this.sessionRepository.Update (session);
		}
	}
}
