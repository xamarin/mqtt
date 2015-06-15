﻿using System.Threading.Tasks;
using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Storage;

namespace System.Net.Mqtt.Flows
{
	internal class ServerConnectFlow : IProtocolFlow
	{
		static readonly ITracer tracer = Tracer.Get<ServerConnectFlow> ();

		readonly IAuthenticationProvider authenticationProvider;
		readonly IRepository<ClientSession> sessionRepository;
		readonly IRepository<ConnectionWill> willRepository;
		readonly IPublishSenderFlow senderFlow;

		public ServerConnectFlow (IAuthenticationProvider authenticationProvider,
			IRepository<ClientSession> sessionRepository, 
			IRepository<ConnectionWill> willRepository,
			IPublishSenderFlow senderFlow)
		{
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

			if (!this.authenticationProvider.Authenticate (connect.UserName, connect.Password)) {
				throw new ProtocolConnectionException (ConnectionStatus.BadUserNameOrPassword);
			}

			var session = this.sessionRepository.Get (s => s.ClientId == clientId);
			var sessionPresent = connect.CleanSession ? false : session != null;

			if (connect.CleanSession && session != null) {
				this.sessionRepository.Delete(session);
				session = null;

				tracer.Info (Properties.Resources.Tracer_Server_CleanedOldSession, clientId);
			}

			if (session == null) {
				session = new ClientSession { ClientId = clientId, Clean = connect.CleanSession };

				this.sessionRepository.Create (session);

				tracer.Info (Properties.Resources.Tracer_Server_CreatedSession, clientId);
			} else {
				await this.SendPendingMessagesAsync (session, channel)
					.ConfigureAwait(continueOnCapturedContext: false);
				await this.SendPendingAcknowledgementsAsync (session, channel)
					.ConfigureAwait(continueOnCapturedContext: false);
			}

			if (connect.Will != null) {
				var connectionWill = new ConnectionWill { ClientId = clientId, Will = connect.Will };

				this.willRepository.Create (connectionWill);
			}

			await channel.SendAsync(new ConnectAck (ConnectionStatus.Accepted, sessionPresent))
				.ConfigureAwait(continueOnCapturedContext: false);
		}

		private async Task SendPendingMessagesAsync(ClientSession session, IChannel<IPacket> channel)
		{
			foreach (var pendingMessage in session.GetPendingMessages()) {
				var publish = new Publish(pendingMessage.Topic, pendingMessage.QualityOfService, 
					pendingMessage.Retain, pendingMessage.Duplicated, pendingMessage.PacketId);

				if (pendingMessage.Status == PendingMessageStatus.PendingToSend) {
					session.RemovePendingMessage (pendingMessage);
					this.sessionRepository.Update (session);

					await this.senderFlow.SendPublishAsync (session.ClientId, publish, channel)
						.ConfigureAwait(continueOnCapturedContext: false);
				} else {
					await this.senderFlow.SendPublishAsync (session.ClientId, publish, channel, PendingMessageStatus.PendingToAcknowledge)
						.ConfigureAwait(continueOnCapturedContext: false);
				}
			}
		}

		private async Task SendPendingAcknowledgementsAsync(ClientSession session, IChannel<IPacket> channel)
		{
			foreach (var pendingAcknowledgement in session.GetPendingAcknowledgements()) {
				var ack = default(IFlowPacket);

				if (pendingAcknowledgement.Type == PacketType.PublishReceived)
					ack = new PublishReceived (pendingAcknowledgement.PacketId);
				else if(pendingAcknowledgement.Type == PacketType.PublishRelease)
					ack = new PublishRelease (pendingAcknowledgement.PacketId);

				await this.senderFlow.SendAckAsync (session.ClientId, ack, channel, PendingMessageStatus.PendingToAcknowledge)
					.ConfigureAwait(continueOnCapturedContext: false);
			}
		}
	}
}
