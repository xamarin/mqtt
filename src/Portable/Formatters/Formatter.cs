using System.Threading.Tasks;
using Hermes.Messages;

namespace Hermes.Formatters
{
	public abstract class Formatter<T> : IFormatter
		where T : class, IMessage
	{
		private readonly IChannel<IMessage> reader;
		private readonly IChannel<byte[]> writer;

		public Formatter (IChannel<IMessage> reader, IChannel<byte[]> writer)
		{
			this.reader = reader;
			this.writer = writer;
		}

		protected abstract T Format (byte[] packet);

		protected abstract byte[] Format (T message);

		/// <exception cref="ProtocolException">ProtocolException</exception>
		public async Task ReadAsync (byte[] packet)
		{
			var message = this.Format (packet);

			await this.reader.SendAsync (message);
		}

		/// <exception cref="ProtocolException">ProtocolException</exception>
		public async Task WriteAsync (IMessage message)
		{
			var packet = this.Format (message as T);

			await this.writer.SendAsync (packet);
		}
	}
}
