using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class ServerConnectFlow : IProtocolFlow
	{
		readonly IRepository<ClientSession> sessionRepository;
		readonly IRepository<ConnectionWill> willRepository;
		readonly IRepository<PacketIdentifier> packetIdentifierRepository;
		readonly IPublishFlow publishFlow;

		public ServerConnectFlow (IRepository<ClientSession> sessionRepository, 
			IRepository<ConnectionWill> willRepository,
			IRepository<PacketIdentifier> packetIdentifierRepository,
			IPublishFlow publishFlow)
		{
			this.sessionRepository = sessionRepository;
			this.willRepository = willRepository;
			this.packetIdentifierRepository = packetIdentifierRepository;
			this.publishFlow = publishFlow;
		}

		public async Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel)
		{
			if (input.Type != PacketType.Connect)
				return;

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
				await this.SendSavedMessagesAsync (session, channel);
				await this.SendPendingMessagesAsync (session, channel);
				await this.SendPendingAcknowledgementsAsync (session, channel);
			}

			if (connect.Will != null) {
				var connectionWill = new ConnectionWill { ClientId = clientId, Will = connect.Will };

				this.willRepository.Create (connectionWill);
			}

			await channel.SendAsync(new ConnectAck (ConnectionStatus.Accepted, sessionPresent));
		}

		private async Task SendSavedMessagesAsync(ClientSession session, IChannel<IPacket> channel)
		{
			foreach (var savedMessage in session.SavedMessages) {
				var publish = new Publish(savedMessage.Topic, savedMessage.QualityOfService, 
					retain: false, duplicated: false, packetId: savedMessage.PacketId);

				await this.publishFlow.SendPublishAsync (session.ClientId, publish, channel);
			}
		}

		private async Task SendPendingMessagesAsync(ClientSession session, IChannel<IPacket> channel)
		{
			foreach (var pendingMessage in session.PendingMessages) {
				var publish = new Publish(pendingMessage.Topic, pendingMessage.QualityOfService, 
					pendingMessage.Retain, pendingMessage.Duplicated, pendingMessage.PacketId);

				await this.publishFlow.SendPublishAsync (session.ClientId, publish, channel);
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

				await this.publishFlow.SendAckAsync (session.ClientId, ack, channel);
			}
		}
	}
}
