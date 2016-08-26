using System.Threading.Tasks;
using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Storage;
using System.Net.Mqtt.Server;
using Props = System.Net.Mqtt.Server.Properties;
using System.Net.Mqtt.Exceptions;

namespace System.Net.Mqtt.Flows
{
	internal class ServerConnectFlow : IProtocolFlow
	{
		readonly ITracer tracer;
		readonly IAuthenticationProvider authenticationProvider;
		readonly IRepository<ClientSession> sessionRepository;
		readonly IRepository<ConnectionWill> willRepository;
		readonly IPublishSenderFlow senderFlow;

		public ServerConnectFlow (IAuthenticationProvider authenticationProvider,
			IRepository<ClientSession> sessionRepository,
			IRepository<ConnectionWill> willRepository,
			IPublishSenderFlow senderFlow,
			ITracerManager tracerManager)
		{
			tracer = tracerManager.Get<ServerConnectFlow> ();
			this.authenticationProvider = authenticationProvider;
			this.sessionRepository = sessionRepository;
			this.willRepository = willRepository;
			this.senderFlow = senderFlow;
		}

		public async Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel)
		{
			if (input.Type != PacketType.Connect)
				return;

			var connect = input as Connect;

			if (!authenticationProvider.Authenticate (connect.UserName, connect.Password)) {
				throw new MqttConnectionException (ConnectionStatus.BadUserNameOrPassword);
			}

			var session = sessionRepository.Get (s => s.ClientId == clientId);
			var sessionPresent = connect.CleanSession ? false : session != null;

			if (connect.CleanSession && session != null) {
				sessionRepository.Delete (session);
				session = null;

				tracer.Info (Props.Resources.Tracer_Server_CleanedOldSession, clientId);
			}

			if (session == null) {
				session = new ClientSession { ClientId = clientId, Clean = connect.CleanSession };

				sessionRepository.Create (session);

				tracer.Info (Props.Resources.Tracer_Server_CreatedSession, clientId);
			} else {
				await SendPendingMessagesAsync (session, channel)
					.ConfigureAwait (continueOnCapturedContext: false);
				await SendPendingAcknowledgementsAsync (session, channel)
					.ConfigureAwait (continueOnCapturedContext: false);
			}

			if (connect.Will != null) {
				var connectionWill = new ConnectionWill { ClientId = clientId, Will = connect.Will };

				willRepository.Create (connectionWill);
			}

			await channel.SendAsync (new ConnectAck (ConnectionStatus.Accepted, sessionPresent))
				.ConfigureAwait (continueOnCapturedContext: false);
		}

		async Task SendPendingMessagesAsync (ClientSession session, IChannel<IPacket> channel)
		{
			foreach (var pendingMessage in session.GetPendingMessages ()) {
				var publish = new Publish(pendingMessage.Topic, pendingMessage.QualityOfService,
					pendingMessage.Retain, pendingMessage.Duplicated, pendingMessage.PacketId);

				if (pendingMessage.Status == PendingMessageStatus.PendingToSend) {
					session.RemovePendingMessage (pendingMessage);
					sessionRepository.Update (session);

					await senderFlow.SendPublishAsync (session.ClientId, publish, channel)
						.ConfigureAwait (continueOnCapturedContext: false);
				} else {
					await senderFlow.SendPublishAsync (session.ClientId, publish, channel, PendingMessageStatus.PendingToAcknowledge)
						.ConfigureAwait (continueOnCapturedContext: false);
				}
			}
		}

		async Task SendPendingAcknowledgementsAsync (ClientSession session, IChannel<IPacket> channel)
		{
			foreach (var pendingAcknowledgement in session.GetPendingAcknowledgements ()) {
				var ack = default(IFlowPacket);

				if (pendingAcknowledgement.Type == PacketType.PublishReceived)
					ack = new PublishReceived (pendingAcknowledgement.PacketId);
				else if (pendingAcknowledgement.Type == PacketType.PublishRelease)
					ack = new PublishRelease (pendingAcknowledgement.PacketId);

				await senderFlow.SendAckAsync (session.ClientId, ack, channel, PendingMessageStatus.PendingToAcknowledge)
					.ConfigureAwait (continueOnCapturedContext: false);
			}
		}
	}
}
