using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using System.Timers;
using Hermes.Flows;
using Hermes.Formatters;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;
using ReactiveSockets;

namespace Hermes
{
	public class ProtocolConnection : IProtocolConnection
	{
		readonly IReactiveSocket socket;
		readonly IEventStream events;
		readonly IProtocolFlowProvider flowProvider;
		readonly IClientManager clientManager;
		readonly IRepository<ConnectionWill> willRepository;
		IPacketManager manager;
		Timer connectTimer;

		/// <exception cref="ProtocolException">ProtocolException</exception>
		public ProtocolConnection (IReactiveSocket socket, IEventStream events, IProtocolConfiguration configuration, 
			IProtocolFlowProvider flowProvider, IClientManager clientManager, IRepository<ConnectionWill> willRepository)
		{
			this.socket = socket;
			this.events = events;
			this.flowProvider = flowProvider;
			this.clientManager = clientManager;
			this.willRepository = willRepository;

			this.Initialize (configuration);
		}

		public Guid Id { get; private set; }

		public string ClientId { get; private set; }

		public bool IsPending { get; private set; }

		public void Confirm (string clientId)
		{
			this.ClientId = clientId;
			this.IsPending = false;
			this.clientManager.Add (this);
			this.connectTimer.Stop ();
		}

		public async Task SendAsync (IPacket packet)
		{
			await this.manager.ManageAsync (packet);
		}

		public void Disconnect ()
		{
			this.socket.Dispose ();
			this.IsPending = true;
			this.ClientId = null;
			this.connectTimer.Stop ();
		}

		private void Initialize (IProtocolConfiguration configuration)
		{
			this.Id = Guid.NewGuid ();
			this.IsPending = true;

			var binaryChannel = new BinaryChannel (this.socket);
			var packetChannel = new PacketChannel (this.events);
			var formatters = this.GetFormatters (packetChannel, binaryChannel);
			
			this.manager =  new PacketManager (formatters);

			binaryChannel.Receiver.Subscribe (async packet => {
				try {
					await this.manager.ManageAsync (packet);
				} catch (ConnectProtocolException connEx) {
					var flow = this.flowProvider.Get (PacketType.Connect) as ConnectFlow;
					var errorPacket = flow.GetError (connEx.ReturnCode);

					this.SendAsync(errorPacket).ContinueWith(t => this.ForceDisconnect ()).Wait();
				} catch (ProtocolException) {
					this.ForceDisconnect ();
				}
			});

			packetChannel.Receiver.Subscribe(async packet => {
				var flow = this.flowProvider.Get (packet.Type);
				var output = flow.Apply (packet, this);

				if (output != null) {
					await this.SendAsync (output);
				}
			});

			this.connectTimer = new Timer (configuration.ConnectTimeWindow);
			this.connectTimer.Elapsed += (sender, e) => {
				if (this.IsPending) {
					throw new ProtocolException (Resources.ProtocolConnection_ConnectNotReceived);
				} else {
					this.connectTimer.Stop ();
				}
			};
			this.connectTimer.Start ();
		}

		private IEnumerable<IFormatter> GetFormatters(IChannel<IPacket> reader, IChannel<byte[]> writer)
		{
			var formatters = new List<IFormatter> ();
			
			formatters.Add (new ConnectFormatter (reader, writer));
			formatters.Add (new ConnectAckFormatter (reader, writer));
			formatters.Add (new PublishFormatter (reader, writer));
			formatters.Add (new FlowPacketFormatter<PublishAck>(PacketType.PublishAck, id => new PublishAck(id), reader, writer));
			formatters.Add (new FlowPacketFormatter<PublishReceived>(PacketType.PublishReceived, id => new PublishReceived(id), reader, writer));
			formatters.Add (new FlowPacketFormatter<PublishRelease>(PacketType.PublishRelease, id => new PublishRelease(id), reader, writer));
			formatters.Add (new FlowPacketFormatter<PublishComplete>(PacketType.PublishComplete, id => new PublishComplete(id), reader, writer));
			formatters.Add (new SubscribeFormatter (reader, writer));
			formatters.Add (new SubscribeAckFormatter (reader, writer));
			formatters.Add (new UnsubscribeFormatter (reader, writer));
			formatters.Add (new FlowPacketFormatter<UnsubscribeAck> (PacketType.UnsubscribeAck, id => new UnsubscribeAck(id), reader, writer));
			formatters.Add (new EmptyPacketFormatter<PingRequest> (PacketType.PingRequest, reader, writer));
			formatters.Add (new EmptyPacketFormatter<PingResponse> (PacketType.PingResponse, reader, writer));
			formatters.Add (new EmptyPacketFormatter<Disconnect> (PacketType.Disconnect, reader, writer));

			return formatters;
		}

		private void ForceDisconnect()
		{
			var connectionWill = this.willRepository.Get (w => w.ConnectionId == this.Id);

			if (connectionWill != null) {
				var will = connectionWill.Will;
				var packetId = default(Nullable<ushort>);

				if (will.QualityOfService != QualityOfService.AtMostOnce) {
					//TODO: Replace this by getting the packet id where the id is not in a storage of used packet ids
					packetId = (ushort)new Random ().Next (0, UInt16.MaxValue);
				}

				var publish = new Publish(will.Topic, will.QualityOfService, will.Retain, duplicatedDelivery: false, packetId: packetId);

				//Will Topic payload consists only of the data portion of the message, not the first two length bytes.
				publish.Payload = Protocol.Encoding.EncodeString (will.Message).Bytes (3);

				this.SendAsync(publish).Wait ();
			}

			this.Disconnect ();
		}
	}
}
