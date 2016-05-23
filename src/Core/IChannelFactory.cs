using System.Threading.Tasks;

namespace System.Net.Mqtt
{
	public interface IChannelFactory
	{
		Task<IChannel<byte[]>> CreateAsync ();
	}
}
