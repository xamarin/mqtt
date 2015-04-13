using Hermes;

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

			client.ConnectAsync (new ClientCredentials (this.GetClientId ())).Wait ();

			return client;
		}
	}
}
