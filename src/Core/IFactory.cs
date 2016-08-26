namespace System.Net.Mqtt
{
	public interface IFactory<T> where T : class
	{
		T Create (ProtocolConfiguration configuration);
	}
}
