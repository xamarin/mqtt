using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes.Flows
{
	public abstract class PublishFlow : IPublishFlow
	{
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
				status == PendingMessageStatus.PendingToSend)
				this.SavePendingAcknowledgement (ack, clientId);

			if (!channel.IsConnected)
				return;

			await channel.SendAsync (ack);

			if(ack.Type == PacketType.PublishReceived)
				await this.MonitorAckAsync<PublishRelease> (ack, channel);
			else if (ack.Type == PacketType.PublishRelease)
				await this.MonitorAckAsync<PublishComplete> (ack, channel);
		}

		protected void RemovePendingAcknowledgement(string clientId, ushort packetId, PacketType type)
		{
			var session = this.sessionRepository.Get (s => s.ClientId == clientId);
			var pendingAcknowledgement = session.PendingAcknowledgements
				.FirstOrDefault(u => u.Type == type && u.PacketId == packetId);

			session.PendingAcknowledgements.Remove (pendingAcknowledgement);

			this.sessionRepository.Update (session);
		}

		protected async Task MonitorAckAsync<T>(IFlowPacket sentMessage, IChannel<IPacket> channel)
			where T : IFlowPacket
		{
			await this.GetAckMonitor<T> (sentMessage, channel);
		}

		protected IObservable<T> GetAckMonitor<T>(IFlowPacket sentMessage, IChannel<IPacket> channel, int retries = 0)
			where T : IFlowPacket
		{
			if (retries == this.configuration.QualityOfServiceAckRetries) {
				throw new ProtocolException (string.Format(Resources.PublishFlow_AckMonitor_ExceededMaximumAckRetries, this.configuration.QualityOfServiceAckRetries));
			}

			return channel.Receiver.OfType<T> ()
				.FirstOrDefaultAsync (x => x.PacketId == sentMessage.PacketId)
				.Timeout (TimeSpan.FromSeconds (this.configuration.WaitingTimeoutSecs))
				.Catch<T, TimeoutException> (timeEx => {
					channel.SendAsync (sentMessage).Wait ();

					return this.GetAckMonitor<T> (sentMessage, channel, retries + 1);
				});
		}

		private void SavePendingAcknowledgement(IFlowPacket ack, string clientId)
		{
			if (ack.Type != PacketType.PublishReceived && ack.Type != PacketType.PublishRelease)
				return;

			var unacknowledgeMessage = new PendingAcknowledgement {
				PacketId = ack.PacketId,
				Type = ack.Type
			};
			
			var session = this.sessionRepository.Get (s => s.ClientId == clientId);

			session.PendingAcknowledgements.Add (unacknowledgeMessage);

			this.sessionRepository.Update (session);
		}
	}
}
