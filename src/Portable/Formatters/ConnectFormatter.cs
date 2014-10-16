using System;
using Hermes.Messages;

namespace Hermes.Formatters
{
	public class ConnectFormatter : FormatterBase<Connect>
	{
		public ConnectFormatter (IChannel<IMessage> reader, IChannel<byte[]> writer)
			: base(reader, writer)
		{
		}

		protected override Connect Format (byte[] packet)
		{
			throw new NotImplementedException ();
		}

		protected override byte[] Format (Connect message)
		{
			throw new NotImplementedException ();
		}
	}
}
