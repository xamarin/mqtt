using System;
using Hermes;
using Hermes.Packets;

namespace IntegrationTests.Context
{
	public class IntegrationFixture : IDisposable
	{
		public IntegrationFixture ()
		{
		}

		public ProtocolConfiguration Configuration { get; private set; }

		public Server Server { get; private set; }

		public void Initialize(ushort keepAliveSecs = 0)
		{
			if (this.Configuration == null) {
				this.Configuration = new ProtocolConfiguration {
					BufferSize = 128 * 1024,
					Port = Protocol.DefaultNonSecurePort,
					KeepAliveSecs = keepAliveSecs,
					WaitingTimeoutSecs = 10,
					MaximumQualityOfService = QualityOfService.ExactlyOnce
				};
			}

			if (Server == null) {
				var initializer = new ServerInitializer ();

				this.Server = initializer.Initialize (this.Configuration);
				this.Server.Start ();
			}
		}

		public void Dispose ()
		{
			this.Server.Stop ();
		}
	}
}
