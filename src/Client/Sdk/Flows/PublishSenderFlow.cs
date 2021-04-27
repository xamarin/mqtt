using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mqtt.Sdk.Packets;
using System.Net.Mqtt.Sdk.Storage;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Sdk.Flows
{
	internal class PublishSenderFlow : PublishFlow, IPublishSenderFlow
	{
		static readonly ITracer tracer = Tracer.Get<PublishSenderFlow> ();

		IDictionary<MqttPacketType, Func<string, ushort, IFlowPacket>> senderRules;

		public PublishSenderFlow (IRepository<ClientSession> sessionRepository,
			MqttConfiguration configuration)
			: base (sessionRepository, configuration)
		{
			DefineSenderRules ();
		}

		public override async Task ExecuteAsync (string clientId, IPacket input, IMqttChannel<IPacket> channel)
		{
			var senderRule = default (Func<string, ushort, IFlowPacket>);

			if (!senderRules.TryGetValue (input.Type, out senderRule)) {
				return;
			}

			var flowPacket = input as IFlowPacket;

			if (flowPacket == null) {
				return;
			}

			var ackPacket = senderRule (clientId, flowPacket.PacketId);

			if (ackPacket != default (IFlowPacket)) {
				await SendAckAsync (clientId, ackPacket, channel)
					.ConfigureAwait (continueOnCapturedContext: false);
			}
		}

		public async Task SendPublishAsync (string clientId, Publish message, IMqttChannel<IPacket> channel, PendingMessageStatus status = PendingMessageStatus.PendingToSend)
		{
			if (channel == null || !channel.IsConnected) {
				SaveMessage (message, clientId, PendingMessageStatus.PendingToSend);
				return;
			}

			var qos = configuration.GetSupportedQos(message.QualityOfService);

			if (qos != MqttQualityOfService.AtMostOnce && status == PendingMessageStatus.PendingToSend) {
				SaveMessage (message, clientId, PendingMessageStatus.PendingToAcknowledge);
			}

			await channel
				.SendAsync (message)
				.ConfigureAwait (continueOnCapturedContext: false);

			if (qos == MqttQualityOfService.AtLeastOnce) {
				await MonitorAckAsync<PublishAck> (message, clientId, channel)
					.ConfigureAwait (continueOnCapturedContext: false);
			} else if (qos == MqttQualityOfService.ExactlyOnce) {
				await MonitorAckAsync<PublishReceived> (message, clientId, channel).ConfigureAwait (continueOnCapturedContext: false);
				await channel
                    .ReceiverStream
					.OfType<PublishComplete> ()
					.FirstOrDefaultAsync (x => x.PacketId == message.PacketId.Value);
			}
		}

		protected void RemovePendingMessage (string clientId, ushort packetId)
		{
			var session = sessionRepository.Read (clientId);

			if (session == null) {
				throw new MqttException (string.Format (Properties.Resources.SessionRepository_ClientSessionNotFound, clientId));
			}

			var pendingMessage = session
				.GetPendingMessages()
				.FirstOrDefault(p => p.PacketId.HasValue && p.PacketId.Value == packetId);

			session.RemovePendingMessage (pendingMessage);

			sessionRepository.Update (session);
		}

		protected async Task MonitorAckAsync<T> (Publish sentMessage, string clientId, IMqttChannel<IPacket> channel)
			where T : IFlowPacket
		{
			var intervalSubscription = Observable
				.Interval (TimeSpan.FromSeconds (configuration.WaitTimeoutSecs), TaskPoolScheduler.Default)
				.Subscribe (async _ => {
					if (channel.IsConnected) {
						tracer.Warn (Properties.Resources.PublishFlow_RetryingQoSFlow, sentMessage.Type, clientId);

						var duplicated = new Publish (sentMessage.Topic, sentMessage.QualityOfService,
							sentMessage.Retain, duplicated: true, packetId: sentMessage.PacketId) {
							Payload = sentMessage.Payload
						};

						await channel.SendAsync (duplicated);
					}
				});

			await channel
                .ReceiverStream
				.OfType<T> ()
				.FirstOrDefaultAsync (x => x.PacketId == sentMessage.PacketId.Value);

			intervalSubscription.Dispose ();
		}

		void DefineSenderRules ()
		{
			senderRules = new Dictionary<MqttPacketType, Func<string, ushort, IFlowPacket>> ();

			senderRules.Add (MqttPacketType.PublishAck, (clientId, packetId) => {
				RemovePendingMessage (clientId, packetId);

				return default (IFlowPacket);
			});

			senderRules.Add (MqttPacketType.PublishReceived, (clientId, packetId) => {
				RemovePendingMessage (clientId, packetId);

				return new PublishRelease (packetId);
			});

			senderRules.Add (MqttPacketType.PublishComplete, (clientId, packetId) => {
				RemovePendingAcknowledgement (clientId, packetId, MqttPacketType.PublishRelease);

				return default (IFlowPacket);
			});
		}

		void SaveMessage (Publish message, string clientId, PendingMessageStatus status)
		{
			if (message.QualityOfService == MqttQualityOfService.AtMostOnce) {
				return;
			}

			var session = sessionRepository.Read (clientId);

			if (session == null) {
				throw new MqttException (string.Format (Properties.Resources.SessionRepository_ClientSessionNotFound, clientId));
			}

			var savedMessage = new PendingMessage {
				Status = status,
				QualityOfService = message.QualityOfService,
				Duplicated = message.Duplicated,
				Retain = message.Retain,
				Topic = message.Topic,
				PacketId = message.PacketId,
				Payload = message.Payload
			};

			session.AddPendingMessage (savedMessage);

			sessionRepository.Update (session);
		}
	}
}
