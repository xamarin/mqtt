using System;
using Hermes.Messages;

namespace Hermes.Formatters
{
	public class PingResponseFormatter : Formatter<PingResponse>
	{
		public PingResponseFormatter (IChannel<IMessage> reader, IChannel<byte[]> writer)
			: base(reader, writer)
		{
		}

		public override MessageType MessageType { get { return Messages.MessageType.PingResponse; } }

		protected override PingResponse Read (byte[] packet)
		{
			return new PingResponse ();
		}

		protected override byte[] Write (PingResponse message)
		{
			var flags = 0x00;
			var type = Convert.ToInt32(MessageType.PingResponse) << 4;

			var fixedHeaderByte1 = Convert.ToByte(flags | type);
			var fixedHeaderByte2 = Convert.ToByte (0x00);

			return new byte[] { fixedHeaderByte1, fixedHeaderByte2 };
		}
	}
}
