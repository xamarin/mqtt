using Hermes;

namespace IntegrationTests.Context
{
	public abstract class ConnectedContext : IntegrationContext
	{
		protected override Client GetClient ()
		{
			var client = base.GetClient ();

			client.ConnectAsync (new ClientCredentials (this.GetClientId ())).Wait ();

			return client;
		}
	}
}
