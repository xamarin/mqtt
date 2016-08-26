using System.Threading.Tasks;

namespace System.Net.Mqtt
{
	public interface IMqttEndpointFactory<T>
	{
		Task<T> CreateAsync (MqttConfiguration configuration);
	}
}
