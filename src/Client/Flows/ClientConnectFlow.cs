using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class ClientConnectFlow : IProtocolFlow
	{
		readonly IRepository<ClientSession> sessionRepository;
		readonly IPublishSenderFlow senderFlow;

		public ClientConnectFlow (IRepository<ClientSession> sessionRepository, IPublishSenderFlow senderFlow)
		{
			this.sessionRepository = sessionRepository;
			this.senderFlow = senderFlow;
		}

		public async Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel)
		{
			if (input.Type != PacketType.ConnectAck)
				return;

			var session = this.sessionRepository.Get (s => s.ClientId == clientId);

			if (session != null) {
				await this.SendPendingMessagesAsync (session, channel);
				await this.SendPendingAcknowledgementsAsync (session, channel);
			}
		}

		private async Task SendPendingMessagesAsync(ClientSession session, IChannel<IPacket> channel)
		{
			foreach (var pendingMessage in session.PendingMessages) {
				var publish = new Publish(pendingMessage.Topic, pendingMessage.QualityOfService, 
					pendingMessage.Retain, pendingMessage.Duplicated, pendingMessage.PacketId);

				await this.senderFlow.SendPublishAsync (session.ClientId, publish, channel);
			}
		}

		private async Task SendPendingAcknowledgementsAsync(ClientSession session, IChannel<IPacket> channel)
		{
			foreach (var unacknowledgeMessage in session.PendingAcknowledgements) {
				var ack = default(IFlowPacket);

				if (unacknowledgeMessage.Type == PacketType.PublishReceived)
					ack = new PublishReceived (unacknowledgeMessage.PacketId);
				else if(unacknowledgeMessage.Type == PacketType.PublishRelease)
					ack = new PublishRelease (unacknowledgeMessage.PacketId);

				await this.senderFlow.SendAckAsync (session.ClientId, ack, channel);
			}
		}
	}
}
