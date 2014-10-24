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
	using Hermes.Messages;
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
				var messageChannel = new MessageChannel (events);
				var formatters = this.GetFormatters (messageChannel, binaryChannel);
				var manager =  new MessageManager (formatters);

				binaryChannel.Received.Subscribe (async packet => {
					await manager.ManageAsync (packet);
				});

				messageChannel.Received.Subscribe(async message => {
					var output = Protocol.Flow.Get (message.Type).Apply (message);

					await manager.ManageAsync (output);
				});
			});

			this.server.Start ();
		}

		private IEnumerable<IFormatter> GetFormatters(IChannel<IMessage> reader, IChannel<byte[]> writer)
		{
			var formatters = new List<IFormatter> ();
			
			formatters.Add (new ConnectFormatter (reader, writer));
			formatters.Add (new ConnectAckFormatter (reader, writer));
			formatters.Add (new PublishFormatter (reader, writer));
			formatters.Add (new FlowMessageFormatter<PublishAck>(MessageType.PublishAck, id => new PublishAck(id), reader, writer));
			formatters.Add (new FlowMessageFormatter<PublishReceived>(MessageType.PublishReceived, id => new PublishReceived(id), reader, writer));
			formatters.Add (new FlowMessageFormatter<PublishRelease>(MessageType.PublishRelease, id => new PublishRelease(id), reader, writer));
			formatters.Add (new FlowMessageFormatter<PublishComplete>(MessageType.PublishComplete, id => new PublishComplete(id), reader, writer));
			formatters.Add (new SubscribeFormatter (reader, writer));
			formatters.Add (new SubscribeAckFormatter (reader, writer));
			formatters.Add (new UnsubscribeFormatter (reader, writer));
			formatters.Add (new FlowMessageFormatter<UnsubscribeAck> (MessageType.UnsubscribeAck, id => new UnsubscribeAck(id), reader, writer));
			formatters.Add (new EmptyMessageFormatter<PingRequest> (MessageType.PingRequest, reader, writer));
			formatters.Add (new EmptyMessageFormatter<PingResponse> (MessageType.PingResponse, reader, writer));
			formatters.Add (new EmptyMessageFormatter<Disconnect> (MessageType.Disconnect, reader, writer));

			return formatters;
		}
	}
}
