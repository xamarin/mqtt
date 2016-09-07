namespace System.Net.Mqtt
{
	public interface IMqttChannelListener : IDisposable
	{
		IObservable<IMqttChannel<byte[]>> GetChannelStream ();
	}
}
