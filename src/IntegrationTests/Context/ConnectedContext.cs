using System.Net.Mqtt.Client;
using System.Threading.Tasks;

namespace IntegrationTests.Context
{
    public abstract class ConnectedContext : IntegrationContext
	{
		public ConnectedContext (ushort keepAliveSecs = 0)
			: base(keepAliveSecs)
		{
		}

		protected override async Task<Client> GetClientAsync ()
		{
			var client = await base.GetClientAsync ();

			client.ConnectAsync (new ClientCredentials (GetClientId ())).Wait ();

			return client;
		}
	}
}
