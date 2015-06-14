namespace System.Net.Mqtt
{
	public interface IAuthenticationProvider
	{
		bool Authenticate (string username, string password);
	}
}
