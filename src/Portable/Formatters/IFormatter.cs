using System.Threading.Tasks;
using Hermes.Messages;

namespace Hermes.Formatters
{
	public interface IFormatter
	{
		/// <summary>
		/// Gets the type of message that this formatter support.
		/// </summary>
		MessageType MessageType { get; }

		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task ReadAsync (byte[] packet);

		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task WriteAsync (IMessage message);
	}
}
