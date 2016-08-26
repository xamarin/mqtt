namespace System.Net.Mqtt
{
	public interface IMqttChannelProvider : IDisposable
	{
		IObservable<IMqttChannel<byte[]>> GetChannels ();
	}
}
