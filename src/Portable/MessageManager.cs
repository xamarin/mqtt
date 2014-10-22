using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hermes.Formatters;
using Hermes.Messages;
using Hermes.Properties;

namespace Hermes
{
	public class MessageManager : IMessageManager
	{
		private readonly Dictionary<MessageType, IFormatter> formatters;

		public MessageManager (params IFormatter[] formatters)
			: this((IEnumerable<IFormatter>)formatters)
		{
		}

		public MessageManager (IEnumerable<IFormatter> formatters)
		{
			this.formatters = formatters.ToDictionary(f => f.MessageType);
		}

		/// <exception cref="ProtocolException">ProtocolException</exception>
		public async Task ManageAsync (byte[] packet)
		{
			var messageType = (MessageType)packet.Byte (0).Bits (4);
			IFormatter formatter;

			if (!formatters.TryGetValue(messageType, out formatter))
				throw new ProtocolException (Resources.MessageManager_PacketUnknown);

			await formatter.ReadAsync (packet);
		}

		/// <exception cref="ProtocolException">ProtocolException</exception>
		public async Task ManageAsync (IMessage message)
		{
			IFormatter formatter;

			if (!formatters.TryGetValue(message.Type, out formatter))
				throw new ProtocolException (Resources.MessageManager_MessageUnknown);

			await formatter.WriteAsync (message);
		}
	}
}
