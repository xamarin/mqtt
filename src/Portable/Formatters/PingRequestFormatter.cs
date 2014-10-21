using System;
using Hermes.Messages;

namespace Hermes.Formatters
{
	public class PingRequestFormatter : Formatter<PingRequest>
	{
		public PingRequestFormatter (IChannel<IMessage> reader, IChannel<byte[]> writer)
			: base(reader, writer)
		{
		}

		protected override PingRequest Format (byte[] packet)
		{
			return new PingRequest ();
		}

		protected override byte[] Format (PingRequest message)
		{
			var flags = 0x00;
			var type = Convert.ToInt32(MessageType.PingRequest) << 4;

			var fixedHeaderByte1 = Convert.ToByte(flags | type);
			var fixedHeaderByte2 = Convert.ToByte (0x00);

			return new byte[] { fixedHeaderByte1, fixedHeaderByte2 };
		}
	}
}
