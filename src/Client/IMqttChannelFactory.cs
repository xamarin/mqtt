using System.Threading.Tasks;

namespace System.Net.Mqtt
{
	public interface IMqttChannelFactory
	{
		Task<IMqttChannel<byte[]>> CreateAsync ();
	}
}
