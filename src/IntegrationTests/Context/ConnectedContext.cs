using System.Net.Mqtt;
using System.Threading.Tasks;

namespace IntegrationTests.Context
{
	public abstract class ConnectedContext : IntegrationContext
	{
		public ConnectedContext (ushort keepAliveSecs = 0, bool allowWildcardsInTopicFilters = true)
			: base (keepAliveSecs, allowWildcardsInTopicFilters)
		{
		}

		public bool CleanSession { get; set; }

		protected override async Task<IMqttClient> GetClientAsync ()
		{
			var client = await base.GetClientAsync ();

			await client.ConnectAsync (new MqttClientCredentials (GetClientId ()), cleanSession: CleanSession);

			return client;
		}
	}
}
