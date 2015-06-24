namespace System.Net.Mqtt.Server
{
	public interface IAuthenticationProvider
	{
		bool Authenticate (string username, string password);
	}
}
