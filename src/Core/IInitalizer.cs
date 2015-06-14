namespace System.Net.Mqtt
{
	public interface IInitalizer<T> where T : class
	{
		T Initialize (ProtocolConfiguration configuration);
	}
}
