namespace Hermes
{
	internal class NoAuthenticationProvider : IAuthenticationProvider
	{
		public bool Authenticate (string username, string password)
		{
			return true;
		}
	}
}
