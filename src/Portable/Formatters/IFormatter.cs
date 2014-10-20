using System.Threading.Tasks;
using Hermes.Messages;

namespace Hermes.Formatters
{
	public interface IFormatter
	{
		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task ReadAsync (byte[] packet);

		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task WriteAsync (IMessage message);
	}
}
