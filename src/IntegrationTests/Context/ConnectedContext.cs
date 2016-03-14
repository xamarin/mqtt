using System.Net.Mqtt.Client;

namespace IntegrationTests.Context
{
	public abstract class ConnectedContext : IntegrationContext
	{
		public ConnectedContext (ushort keepAliveSecs = 0)
			: base(keepAliveSecs)
		{
		}

		protected override Client GetClient ()
		{
			var client = base.GetClient ();

			client.ConnectAsync (new ClientCredentials (GetClientId ())).Wait ();

			return client;
		}
	}
}
