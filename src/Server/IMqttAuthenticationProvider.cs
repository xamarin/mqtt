namespace System.Net.Mqtt.Server
{
	public interface IMqttAuthenticationProvider
	{
		bool Authenticate (string username, string password);
	}
}
