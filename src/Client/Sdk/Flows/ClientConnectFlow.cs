using System.Threading.Tasks;
using System.Net.Mqtt.Sdk.Packets;
using System.Net.Mqtt.Sdk.Storage;

namespace System.Net.Mqtt.Sdk.Flows
{
	internal class ClientConnectFlow : IProtocolFlow
	{
		readonly IRepository<ClientSession> sessionRepository;
		readonly IPublishSenderFlow senderFlow;

		public ClientConnectFlow (IRepository<ClientSession> sessionRepository,
			IPublishSenderFlow senderFlow)
		{
			this.sessionRepository = sessionRepository;
			this.senderFlow = senderFlow;
		}

		public async Task ExecuteAsync (string clientId, IPacket input, IMqttChannel<IPacket> channel)
		{
			if (input.Type != MqttPacketType.ConnectAck) {
				return;
			}

			var ack = input as ConnectAck;

			if (ack.Status != MqttConnectionStatus.Accepted) {
				return;
			}

			var session = sessionRepository.Read (clientId);

			if (session == null) {
				throw new MqttException (string.Format (Properties.Resources.SessionRepository_ClientSessionNotFound, clientId));
			}

			await SendPendingMessagesAsync (session, channel)
				.ConfigureAwait (continueOnCapturedContext: false);
			await SendPendingAcknowledgementsAsync (session, channel)
				.ConfigureAwait (continueOnCapturedContext: false);
		}

		async Task SendPendingMessagesAsync (ClientSession session, IMqttChannel<IPacket> channel)
		{
			foreach (var pendingMessage in session.GetPendingMessages ()) {
				var publish = new Publish(pendingMessage.Topic, pendingMessage.QualityOfService,
					pendingMessage.Retain, pendingMessage.Duplicated, pendingMessage.PacketId);

				await senderFlow
					.SendPublishAsync (session.Id, publish, channel, PendingMessageStatus.PendingToAcknowledge)
					.ConfigureAwait (continueOnCapturedContext: false);
			}
		}

		async Task SendPendingAcknowledgementsAsync (ClientSession session, IMqttChannel<IPacket> channel)
		{
			foreach (var pendingAcknowledgement in session.GetPendingAcknowledgements ()) {
				var ack = default(IFlowPacket);

				if (pendingAcknowledgement.Type == MqttPacketType.PublishReceived) {
					ack = new PublishReceived (pendingAcknowledgement.PacketId);
				} else if (pendingAcknowledgement.Type == MqttPacketType.PublishRelease) {
					ack = new PublishRelease (pendingAcknowledgement.PacketId);
				}

				await senderFlow.SendAckAsync (session.Id, ack, channel)
					.ConfigureAwait (continueOnCapturedContext: false);
			}
		}
	}
}
