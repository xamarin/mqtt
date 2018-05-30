using System.Diagnostics;
using System.Net.Mqtt.Sdk.Packets;
using System.Net.Mqtt.Sdk.Storage;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Sdk.Flows
{
	internal class ServerConnectFlow : IProtocolFlow
	{
		static readonly ITracer tracer = Tracer.Get<ServerConnectFlow> ();

		readonly IMqttAuthenticationProvider authenticationProvider;
		readonly IRepository<ClientSession> sessionRepository;
		readonly IRepository<ConnectionWill> willRepository;
		readonly IPublishSenderFlow senderFlow;

		public ServerConnectFlow (IMqttAuthenticationProvider authenticationProvider,
			IRepository<ClientSession> sessionRepository,
			IRepository<ConnectionWill> willRepository,
			IPublishSenderFlow senderFlow)
		{
			this.authenticationProvider = authenticationProvider;
			this.sessionRepository = sessionRepository;
			this.willRepository = willRepository;
			this.senderFlow = senderFlow;
		}

		public async Task ExecuteAsync (string clientId, IPacket input, IMqttChannel<IPacket> channel)
		{
			if (input.Type != MqttPacketType.Connect)
				return;

			var connect = input as Connect;

			if (!authenticationProvider.Authenticate (clientId, connect.UserName, connect.Password)) {
				throw new MqttConnectionException (MqttConnectionStatus.BadUserNameOrPassword);
			}

			var session = sessionRepository.Read (clientId);
			var sessionPresent = connect.CleanSession ? false : session != null;

			if (connect.CleanSession && session != null) {
				sessionRepository.Delete (session.Id);
				session = null;

				tracer.Info (Server.Properties.Resources.Server_CleanedOldSession, clientId);
			}

			var sendPendingMessages = false;

			if (session == null) {
				session = new ClientSession (clientId, connect.CleanSession);

				sessionRepository.Create (session);

				tracer.Info (Server.Properties.Resources.Server_CreatedSession, clientId);
			} else {
				sendPendingMessages = true;
			}

			if (connect.Will != null) {
				var connectionWill = new ConnectionWill (clientId, connect.Will);

				willRepository.Create (connectionWill);
			}

			await channel.SendAsync (new ConnectAck (MqttConnectionStatus.Accepted, sessionPresent))
				.ConfigureAwait (continueOnCapturedContext: false);

			if (sendPendingMessages) {
				await SendPendingMessagesAsync (session, channel)
					.ConfigureAwait (continueOnCapturedContext: false);
				await SendPendingAcknowledgementsAsync (session, channel)
					.ConfigureAwait (continueOnCapturedContext: false);
			}
		}

		async Task SendPendingMessagesAsync (ClientSession session, IMqttChannel<IPacket> channel)
		{
			foreach (var pendingMessage in session.GetPendingMessages ()) {
				var publish = new Publish (pendingMessage.Topic, pendingMessage.QualityOfService,
					pendingMessage.Retain, pendingMessage.Duplicated, pendingMessage.PacketId)
				{
					Payload = pendingMessage.Payload
				};

				if (pendingMessage.Status == PendingMessageStatus.PendingToSend) {
					session.RemovePendingMessage (pendingMessage);
					sessionRepository.Update (session);

					await senderFlow.SendPublishAsync (session.Id, publish, channel)
						.ConfigureAwait (continueOnCapturedContext: false);
				} else {
					await senderFlow.SendPublishAsync (session.Id, publish, channel, PendingMessageStatus.PendingToAcknowledge)
						.ConfigureAwait (continueOnCapturedContext: false);
				}
			}
		}

		async Task SendPendingAcknowledgementsAsync (ClientSession session, IMqttChannel<IPacket> channel)
		{
			foreach (var pendingAcknowledgement in session.GetPendingAcknowledgements ()) {
				var ack = default(IFlowPacket);

				if (pendingAcknowledgement.Type == MqttPacketType.PublishReceived)
					ack = new PublishReceived (pendingAcknowledgement.PacketId);
				else if (pendingAcknowledgement.Type == MqttPacketType.PublishRelease)
					ack = new PublishRelease (pendingAcknowledgement.PacketId);

				await senderFlow.SendAckAsync (session.Id, ack, channel, PendingMessageStatus.PendingToAcknowledge)
					.ConfigureAwait (continueOnCapturedContext: false);
			}
		}
	}
}
