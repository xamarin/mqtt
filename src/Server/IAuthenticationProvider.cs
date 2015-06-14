namespace Hermes
{
	public interface IAuthenticationProvider
	{
		bool Authenticate (string username, string password);
	}
}
