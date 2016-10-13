namespace System.Net.Mqtt.Sdk
{
	internal class NullAuthenticationProvider : IMqttAuthenticationProvider
	{
		static readonly Lazy<NullAuthenticationProvider> instance;

		static NullAuthenticationProvider ()
		{
			instance = new Lazy<NullAuthenticationProvider> (() => new NullAuthenticationProvider ());
		}

		NullAuthenticationProvider ()
		{
		}

		public static IMqttAuthenticationProvider Instance { get { return instance.Value; } }

		public bool Authenticate (string clientId, string username, string password)
		{
			return true;
		}
	}
}
