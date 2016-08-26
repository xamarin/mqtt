﻿using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Storage;
using System.Reactive.Concurrency;
using System.Net.Mqtt.Exceptions;

namespace System.Net.Mqtt.Flows
{
	internal abstract class PublishFlow : IPublishFlow
	{
		readonly ITracer tracer;
		protected readonly IRepository<ClientSession> sessionRepository;
		protected readonly MqttConfiguration configuration;

		protected PublishFlow (IRepository<ClientSession> sessionRepository,
			ITracerManager tracerManager,
			MqttConfiguration configuration)
		{
			tracer = tracerManager.Get<PublishFlow> ();
			this.sessionRepository = sessionRepository;
			this.configuration = configuration;
		}

		public abstract Task ExecuteAsync (string clientId, IPacket input, IMqttChannel<IPacket> channel);

		public async Task SendAckAsync (string clientId, IFlowPacket ack, IMqttChannel<IPacket> channel, PendingMessageStatus status = PendingMessageStatus.PendingToSend)
		{
			if ((ack.Type == MqttPacketType.PublishReceived || ack.Type == MqttPacketType.PublishRelease) &&
				status == PendingMessageStatus.PendingToSend) {
				SavePendingAcknowledgement (ack, clientId);
			}

			if (!channel.IsConnected) {
				return;
			}

			await channel.SendAsync (ack)
				.ConfigureAwait (continueOnCapturedContext: false);

			if (ack.Type == MqttPacketType.PublishReceived) {
				await MonitorAckAsync<PublishRelease> (ack, clientId, channel)
					.ConfigureAwait (continueOnCapturedContext: false);
			} else if (ack.Type == MqttPacketType.PublishRelease) {
				await MonitorAckAsync<PublishComplete> (ack, clientId, channel)
					.ConfigureAwait (continueOnCapturedContext: false);
			}
		}

		protected void RemovePendingAcknowledgement (string clientId, ushort packetId, MqttPacketType type)
		{
			var session = sessionRepository.Get (s => s.ClientId == clientId);

			if (session == null) {
				throw new MqttException (string.Format (Resources.SessionRepository_ClientSessionNotFound, clientId));
			}

			var pendingAcknowledgement = session
				.GetPendingAcknowledgements()
				.FirstOrDefault(u => u.Type == type && u.PacketId == packetId);

			session.RemovePendingAcknowledgement (pendingAcknowledgement);

			sessionRepository.Update (session);
		}

		protected async Task MonitorAckAsync<T> (IFlowPacket sentMessage, string clientId, IMqttChannel<IPacket> channel)
			where T : IFlowPacket
		{
			var intervalSubscription = Observable
				.Interval (TimeSpan.FromSeconds (configuration.WaitingTimeoutSecs), NewThreadScheduler.Default)
				.Subscribe (async _ => {
					if (channel.IsConnected) {
						tracer.Warn (Resources.Tracer_PublishFlow_RetryingQoSFlow, sentMessage.Type, clientId);

						await channel.SendAsync (sentMessage);
					}
				});

			await channel.Receiver
				.ObserveOn (NewThreadScheduler.Default)
				.OfType<T> ()
				.FirstOrDefaultAsync (x => x.PacketId == sentMessage.PacketId);

			intervalSubscription.Dispose ();
		}

		void SavePendingAcknowledgement (IFlowPacket ack, string clientId)
		{
			if (ack.Type != MqttPacketType.PublishReceived && ack.Type != MqttPacketType.PublishRelease) {
				return;
			}

			var unacknowledgeMessage = new PendingAcknowledgement {
				PacketId = ack.PacketId,
				Type = ack.Type
			};

			var session = sessionRepository.Get (s => s.ClientId == clientId);

			if (session == null) {
				throw new MqttException (string.Format (Resources.SessionRepository_ClientSessionNotFound, clientId));
			}

			session.AddPendingAcknowledgement (unacknowledgeMessage);

			sessionRepository.Update (session);
		}
	}
}
