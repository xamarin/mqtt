using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class PublishSenderFlow : PublishFlow
	{
		readonly IRepository<PacketIdentifier> packetIdentifierRepository;
		
		IDictionary<PacketType, Func<string, ushort, IChannel<IPacket>, IFlowPacket>> senderRules;

		public PublishSenderFlow (ProtocolConfiguration configuration, 
			IClientManager clientManager,
			IRepository<ClientSession> sessionRepository,
			IRepository<PacketIdentifier> packetIdentifierRepository)
			: base(clientManager, sessionRepository, configuration)
		{
			this.packetIdentifierRepository = packetIdentifierRepository;

			this.DefineSenderRules ();
		}

		public override async Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel)
		{
			var senderRule = default (Func<string, ushort, IChannel<IPacket>, IFlowPacket>);

			if (!this.senderRules.TryGetValue (input.Type, out senderRule))
				return;

			var flowPacket = input as IFlowPacket;

			if (flowPacket == null)
				return;

			var ackPacket = senderRule (clientId, flowPacket.PacketId, channel);

			if (ackPacket != default(IFlowPacket)) {
				await this.SendAckAsync (clientId, ackPacket, channel);;
			}
		}

		private void DefineSenderRules ()
		{
			this.senderRules = new Dictionary<PacketType, Func<string, ushort, IChannel<IPacket>, IFlowPacket>> ();

			this.senderRules.Add (PacketType.PublishAck, (clientId, packetId, channel) => {
				var session = this.sessionRepository.Get (s => s.ClientId == clientId);
				var pendingMessage = session.PendingMessages.FirstOrDefault(p => p.PacketId.HasValue 
					&& p.PacketId.Value == packetId);

				session.PendingMessages.Remove (pendingMessage);

				this.sessionRepository.Update (session);

				this.packetIdentifierRepository.Delete (i => i.Value == packetId);

				return default (IFlowPacket);
			});

			this.senderRules.Add (PacketType.PublishReceived, (clientId, packetId, channel) => {
				var session = this.sessionRepository.Get (s => s.ClientId == clientId);
				var pendingMessage = session.PendingMessages.FirstOrDefault(p => p.PacketId.HasValue 
					&& p.PacketId.Value == packetId);

				session.PendingMessages.Remove (pendingMessage);

				this.sessionRepository.Update (session);

				return new PublishRelease(packetId);
			});

			this.senderRules.Add (PacketType.PublishComplete, (clientId, packetId, channel) => {
				var session = this.sessionRepository.Get (s => s.ClientId == clientId);
				var pendingAcknowledgement = session.PendingAcknowledgements
					.FirstOrDefault(u => u.Type == PacketType.PublishRelease &&
						u.PacketId == packetId);

				session.PendingAcknowledgements.Remove (pendingAcknowledgement);

				this.sessionRepository.Update (session);

				this.packetIdentifierRepository.Delete (i => i.Value == packetId);

				return default (IFlowPacket);
			});
		}
	}
}
