using System;
using Hermes.Messages;

namespace Hermes.Formatters
{
	public class DisconnectFormatter : Formatter<Disconnect>
	{
		public DisconnectFormatter (IChannel<IMessage> reader, IChannel<byte[]> writer)
			: base(reader, writer)
		{
		}

		protected override Disconnect Format (byte[] packet)
		{
			return new Disconnect ();
		}

		protected override byte[] Format (Disconnect message)
		{
			var flags = 0x00;
			var type = Convert.ToInt32(MessageType.Disconnect) << 4;

			var fixedHeaderByte1 = Convert.ToByte(flags | type);
			var fixedHeaderByte2 = Convert.ToByte (0x00);

			return new byte[] { fixedHeaderByte1, fixedHeaderByte2 };
		}
	}
}
