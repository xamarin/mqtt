/*
   Copyright 2014 NETFX

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0
*/

namespace Hermes
{
	using System;
	using System.Collections.Generic;
	using System.Reactive;
	using Hermes.Formatters;
	using Hermes.Packets;
	using ReactiveSockets;

	public class Server
	{
		readonly IEventStream events;
		readonly IReactiveListener server;

		public Server(int port)
		{
			this.events = new EventStream ();
			this.server = new ReactiveListener (port);

			this.server.Connections.Subscribe (socket => {
				var binaryChannel = new BinaryChannel (socket);
				var packetChannel = new PacketChannel (events);
				var formatters = this.GetFormatters (packetChannel, binaryChannel);
				var manager =  new PacketManager (formatters);

				binaryChannel.Received.Subscribe (async packet => {
					await manager.ManageAsync (packet);
				});

				packetChannel.Received.Subscribe(async packet => {
					var output = Protocol.Flow.Get (packet.Type).Apply (packet);

					await manager.ManageAsync (output);
				});
			});

			this.server.Start ();
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
	}
}
