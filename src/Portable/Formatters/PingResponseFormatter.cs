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

		protected override bool CanFormat (MessageType messageType)
		{
			return messageType == MessageType.PingResponse;
		}

		protected override PingResponse Format (byte[] packet)
		{
			return new PingResponse ();
		}

		protected override byte[] Format (PingResponse message)
		{
			var flags = 0x00;
			var type = Convert.ToInt32(MessageType.PingResponse) << 4;

			var fixedHeaderByte1 = Convert.ToByte(flags | type);
			var fixedHeaderByte2 = Convert.ToByte (0x00);

			return new byte[] { fixedHeaderByte1, fixedHeaderByte2 };
		}
	}
}
