using System;
using Hermes.Messages;

namespace Hermes.Formatters
{
	public class EmptyMessageFormatter <T> : Formatter<T>
		where T : class, IMessage, new()
	{
		readonly MessageType messageType;

		public EmptyMessageFormatter (MessageType messageType, IChannel<IMessage> reader, IChannel<byte[]> writer)
			: base(reader, writer)
		{
			this.messageType = messageType;
		}

		public override MessageType MessageType { get { return this.messageType; } }

		protected override T Read (byte[] packet)
		{
			this.ValidateHeaderFlag (packet, t => t == this.messageType, 0x00);

			return new T();
		}

		protected override byte[] Write (T message)
		{
			var flags = 0x00;
			var type = Convert.ToInt32(this.messageType) << 4;

			var fixedHeaderByte1 = Convert.ToByte(flags | type);
			var fixedHeaderByte2 = Convert.ToByte (0x00);

			return new byte[] { fixedHeaderByte1, fixedHeaderByte2 };
		}
	}
}
