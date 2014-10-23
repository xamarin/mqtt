/*
   Copyright 2014 NETFX

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0
*/
using System;
using Hermes.Messages;
using Hermes.Properties;

namespace Hermes.Formatters
{
	public class FlowMessageFormatter<T> : Formatter<T>
		where T : class, IFlowMessage
	{
		private readonly MessageType messageType;
		private readonly Func<ushort, T> messageFactory;

		public FlowMessageFormatter(MessageType messageType, Func<ushort, T> messageFactory, IChannel<IMessage> reader, IChannel<byte[]> writer)
			: base(reader, writer)
		{
			this.messageType = messageType;
			this.messageFactory = messageFactory;
		}

		public override MessageType MessageType { get { return messageType; } }

		protected override T Read(byte[] packet)
		{
			this.ValidateHeaderFlag (packet, t => t == MessageType.PublishRelease, 0x02);
			this.ValidateHeaderFlag (packet, t => t != MessageType.PublishRelease, 0x00);

			var remainingLengthBytesLength = 0;
			
			Protocol.Encoding.DecodeRemainingLength (packet, out remainingLengthBytesLength);

			var messageIdIndex = Protocol.PacketTypeLength + remainingLengthBytesLength;
			var messageIdBytes = packet.Bytes (messageIdIndex, 2);

			return messageFactory(messageIdBytes.ToUInt16 ());
		}

		protected override byte[] Write(T message)
		{
			var variableHeader = Protocol.Encoding.EncodeBigEndian(message.MessageId);
			var remainingLength = Protocol.Encoding.EncodeRemainingLength (variableHeader.Length);
			var fixedHeader = this.GetFixedHeader (message.Type, remainingLength);
			var packet = new byte[fixedHeader.Length + variableHeader.Length];

			fixedHeader.CopyTo(packet, 0);
			variableHeader.CopyTo(packet, fixedHeader.Length);

			return packet;
		}

		private byte[] GetFixedHeader(MessageType messageType, byte[] remainingLength)
		{
			// MQTT 2.2.2: http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/csprd02/mqtt-v3.1.1-csprd02.html#_Toc385349758
			// The flags for PUBREL are different than for the other flow messages.
			var flags = messageType == Messages.MessageType.PublishRelease ? 0x02 : 0x00;
			var type = Convert.ToInt32(messageType) << 4;
			var fixedHeaderByte1 = Convert.ToByte(flags | type);
			var fixedHeader = new byte[1 + remainingLength.Length];

			fixedHeader[0] = fixedHeaderByte1;
			remainingLength.CopyTo(fixedHeader, 1);

			return fixedHeader;
		}
	}
}
