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
		private readonly IEnumerable<IFormatter> formatters;

		public MessageManager (params IFormatter[] formatters)
			: this((IEnumerable<IFormatter>)formatters)
		{
		}

		public MessageManager (IEnumerable<IFormatter> formatters)
		{
			this.formatters = formatters;
		}

		/// <exception cref="ProtocolException">ProtocolException</exception>
		public async Task ManageAsync (byte[] packet)
		{
			var formatter = this.formatters.FirstOrDefault (f => f.CanFormat (packet));

			if (formatter == null)
				throw new ProtocolException (Resources.MessageManager_PacketUnknown);

			await formatter.ReadAsync (packet);
		}

		/// <exception cref="ProtocolException">ProtocolException</exception>
		public async Task ManageAsync (IMessage message)
		{
			var formatter = this.formatters.FirstOrDefault (f => f.CanFormat (message));

			if (formatter == null)
				throw new ProtocolException (Resources.MessageManager_MessageUnknown);

			await formatter.WriteAsync (message);
		}
	}
}
