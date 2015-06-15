namespace System.Net.Mqtt.Server
{
	internal class NullAuthenticationProvider : IAuthenticationProvider
	{
		private static readonly Lazy<NullAuthenticationProvider> instance;

		static NullAuthenticationProvider()
		{
			instance = new Lazy<NullAuthenticationProvider> (() => new NullAuthenticationProvider ());
		}

		private NullAuthenticationProvider()
		{
		}

		public static IAuthenticationProvider Instance { get { return instance.Value; } }

		public bool Authenticate (string username, string password)
		{
			return true;
		}
	}
}
