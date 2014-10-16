using System.Threading.Tasks;
using Hermes.Messages;

namespace Hermes.Formatters
{
	public interface IFormatter
	{
		Task ReadAsync (byte[] packet);

		Task WriteAsync (IMessage message);
	}
}
