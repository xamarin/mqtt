using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class ClientConnectFlow : IProtocolFlow
	{
		readonly IRepository<ClientSession> sessionRepository;
		readonly IPublishSenderFlow senderFlow;

		public ClientConnectFlow (IRepository<ClientSession> sessionRepository, 
			IPublishSenderFlow senderFlow)
		{
			this.sessionRepository = sessionRepository;
			this.senderFlow = senderFlow;
		}

		public async Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel)
		{
			if (input.Type != PacketType.ConnectAck) {
				return;
			}

			var session = this.sessionRepository.Get (s => s.ClientId == clientId);

			if (session == null) {
				throw new ProtocolException (string.Format (Resources.SessionRepository_ClientSessionNotFound, clientId));
			}

			await this.SendPendingMessagesAsync (session, channel)
				.ConfigureAwait(continueOnCapturedContext: false);
			await this.SendPendingAcknowledgementsAsync (session, channel)
				.ConfigureAwait(continueOnCapturedContext: false);
		}

		private async Task SendPendingMessagesAsync(ClientSession session, IChannel<IPacket> channel)
		{
			foreach (var pendingMessage in session.GetPendingMessages()) {
				var publish = new Publish(pendingMessage.Topic, pendingMessage.QualityOfService, 
					pendingMessage.Retain, pendingMessage.Duplicated, pendingMessage.PacketId);

				await this.senderFlow
					.SendPublishAsync (session.ClientId, publish, channel, PendingMessageStatus.PendingToAcknowledge)
					.ConfigureAwait(continueOnCapturedContext: false);
			}
		}

		private async Task SendPendingAcknowledgementsAsync(ClientSession session, IChannel<IPacket> channel)
		{
			foreach (var pendingAcknowledgement in session.GetPendingAcknowledgements()) {
				var ack = default(IFlowPacket);

				if (pendingAcknowledgement.Type == PacketType.PublishReceived) {
					ack = new PublishReceived (pendingAcknowledgement.PacketId);
				} else if(pendingAcknowledgement.Type == PacketType.PublishRelease) {
					ack = new PublishRelease (pendingAcknowledgement.PacketId);
				}

				await this.senderFlow.SendAckAsync (session.ClientId, ack, channel)
					.ConfigureAwait(continueOnCapturedContext: false);
			}
		}
	}
}
