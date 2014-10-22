using System.Threading.Tasks;
using Hermes.Messages;
using Hermes.Properties;

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

		protected abstract bool CanFormat (MessageType messageType);

		protected abstract T Format (byte[] packet);

		protected abstract byte[] Format (T message);

		public bool CanFormat (byte[] packet)
		{
			var messageType = (MessageType)packet.Byte (0).Bits (4);

			return this.CanFormat (messageType);
		}

		public bool CanFormat (IMessage message)
		{
			return message.GetType () == typeof (T);
		}

		/// <exception cref="ProtocolException">ProtocolException</exception>
		public async Task ReadAsync (byte[] packet)
		{
			if (!this.CanFormat (packet)) {
				var error = string.Format(Resources.Formatter_InvalidPacket, typeof(T).Name);

				throw new ProtocolException (error);
			}

			var message = this.Format (packet);

			await this.reader.SendAsync (message);
		}

		/// <exception cref="ProtocolException">ProtocolException</exception>
		public async Task WriteAsync (IMessage message)
		{
			if (!this.CanFormat (message)){
				var error = string.Format(Resources.Formatter_InvalidMessage, typeof(T).Name);

				throw new ProtocolException (error);
			}

			var packet = this.Format (message as T);

			await this.writer.SendAsync (packet);
		}
	}
}
