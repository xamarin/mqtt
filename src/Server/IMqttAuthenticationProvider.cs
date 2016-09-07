namespace System.Net.Mqtt
{
	public interface IMqttAuthenticationProvider
	{
		bool Authenticate (string username, string password);
	}
}
