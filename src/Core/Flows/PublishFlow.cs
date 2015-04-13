using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Hermes.Packets;
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
			await channel.Receiver.OfType<T> ()
				.FirstOrDefaultAsync (x => x.PacketId == sentMessage.PacketId)
				.Timeout (TimeSpan.FromSeconds (this.configuration.WaitingTimeoutSecs))
				.Do(_ => {}, async ex => {
					if (ex is TimeoutException) {
						await channel.SendAsync (sentMessage);
					}
				})
				.Retry(this.configuration.QualityOfServiceAckRetries);
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
