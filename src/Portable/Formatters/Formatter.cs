using System.Threading.Tasks;
using Hermes.Messages;
using Hermes.Properties;

namespace Hermes.Formatters
{
	// TODO: remove T parameter here unless we really use the T somewhere?
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

		protected abstract T Read (byte[] packet);

		protected abstract byte[] Write (T message);

		public abstract MessageType MessageType { get; }

		/// <exception cref="ProtocolException">ProtocolException</exception>
		public async Task ReadAsync (byte[] packet)
		{
			var actualType = (MessageType)packet.Byte (0).Bits (4);

			if (MessageType != actualType) {
				var error = string.Format(Resources.Formatter_InvalidPacket, typeof(T).Name);

				throw new ProtocolException (error);
			}

			var message = this.Read (packet);

			await this.reader.SendAsync (message);
		}

		/// <exception cref="ProtocolException">ProtocolException</exception>
		public async Task WriteAsync (IMessage message)
		{
			if (message.Type != MessageType) {
				var error = string.Format(Resources.Formatter_InvalidMessage, typeof(T).Name);

				throw new ProtocolException (error);
			}

			var packet = this.Write (message as T);

			await this.writer.SendAsync (packet);
		}
	}
}
