using System;
using System.Configuration;
using Hermes;
using Hermes.Packets;

namespace IntegrationTests.Context
{
	public abstract class IntegrationContext
	{
		protected readonly ushort keepAliveSecs;

		public IntegrationContext (ushort keepAliveSecs = 0)
		{
			this.keepAliveSecs = keepAliveSecs;
			this.Configuration = new ProtocolConfiguration {
				BufferSize = 128 * 1024,
				Port = Protocol.DefaultNonSecurePort,
				KeepAliveSecs = this.keepAliveSecs,
				WaitingTimeoutSecs = 10,
				MaximumQualityOfService = QualityOfService.ExactlyOnce
			};

			Tracing.Initialize (this.Configuration);
		}

		protected ProtocolConfiguration Configuration { get; private set; }

		protected Server GetServer()
		{
			var initializer = new ServerInitializer ();
			var server = initializer.Initialize (this.Configuration);

			server.Start ();

			return server;
		}

		protected virtual Client GetClient()
		{
			var initializer = new ClientInitializer ("127.0.0.1");
			
			return initializer.Initialize (this.Configuration);
		}

		protected string GetClientId()
		{
			return string.Concat ("Client", Guid.NewGuid ().ToString ().Replace("-", string.Empty).Substring (0, 15));
		}

		protected int GetTestLoad()
		{
			var testLoad = 0;
			var loadValue = ConfigurationManager.AppSettings["testLoad"];

			int.TryParse (loadValue, out testLoad);

			return testLoad;
		}
	}
}
