using System.Threading.Tasks;
using Hermes.Messages;

namespace Hermes
{
	public interface IMessageManager
	{
		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task ManageAsync (byte[] packet);

		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task ManageAsync (IMessage message);
	}
}
