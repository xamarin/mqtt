using System.Collections.Generic;
using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class ServerConnectFlow : IProtocolFlow
	{
		readonly IConnectionProvider connectionProvider;
		readonly IRepository<ClientSession> sessionRepository;
		readonly IRepository<ConnectionWill> willRepository;
		readonly IRepository<PacketIdentifier> packetIdentifierRepository;
		readonly IPublishSenderFlow senderFlow;

		public ServerConnectFlow (IConnectionProvider connectionProvider,
			IRepository<ClientSession> sessionRepository, 
			IRepository<ConnectionWill> willRepository,
			IRepository<PacketIdentifier> packetIdentifierRepository,
			IPublishSenderFlow senderFlow)
		{
			this.connectionProvider = connectionProvider;
			this.sessionRepository = sessionRepository;
			this.willRepository = willRepository;
			this.packetIdentifierRepository = packetIdentifierRepository;
			this.senderFlow = senderFlow;
		}

		public async Task ExecuteAsync (string clientId, IPacket input)
		{
			if (input.Type != PacketType.Connect)
				return;

			var channel = this.connectionProvider.GetConnection (clientId);

			var connect = input as Connect;
			var session = this.sessionRepository.Get (s => s.ClientId == clientId);
			var sessionPresent = connect.CleanSession ? false : session != null;

			if (connect.CleanSession && session != null) {
				this.sessionRepository.Delete(session);
				session = null;
			}

			if (session == null) {
				session = new ClientSession { ClientId = clientId, Clean = connect.CleanSession };

				this.sessionRepository.Create (session);
			} else {
				await this.SendPendingMessagesAsync (session, channel);
				await this.SendPendingAcknowledgementsAsync (session, channel);
			}

			if (connect.Will != null) {
				var connectionWill = new ConnectionWill { ClientId = clientId, Will = connect.Will };

				this.willRepository.Create (connectionWill);
			}

			await channel.SendAsync(new ConnectAck (ConnectionStatus.Accepted, sessionPresent));
		}

		private async Task SendPendingMessagesAsync(ClientSession session, IChannel<IPacket> channel)
		{
			var pendingMessages = new List<PendingMessage> (session.PendingMessages);

			foreach (var pendingMessage in pendingMessages) {
				var publish = new Publish(pendingMessage.Topic, pendingMessage.QualityOfService, 
					pendingMessage.Retain, pendingMessage.Duplicated, pendingMessage.PacketId);

				if (pendingMessage.Status == PendingMessageStatus.PendingToSend) {
					session.PendingMessages.Remove (pendingMessage);
					this.sessionRepository.Update (session);

					await this.senderFlow.SendPublishAsync (session.ClientId, publish);
				} else {
					await this.senderFlow.SendPublishAsync (session.ClientId, publish, PendingMessageStatus.PendingToAcknowledge);
				}
			}
		}

		private async Task SendPendingAcknowledgementsAsync(ClientSession session, IChannel<IPacket> channel)
		{
			var pendingAcknowledgements = new List<PendingAcknowledgement> (session.PendingAcknowledgements);

			foreach (var pendingAcknowledgement in pendingAcknowledgements) {
				var ack = default(IFlowPacket);

				if (pendingAcknowledgement.Type == PacketType.PublishReceived)
					ack = new PublishReceived (pendingAcknowledgement.PacketId);
				else if(pendingAcknowledgement.Type == PacketType.PublishRelease)
					ack = new PublishRelease (pendingAcknowledgement.PacketId);

				await this.senderFlow.SendAckAsync (session.ClientId, ack, PendingMessageStatus.PendingToAcknowledge);
			}
		}
	}
}
