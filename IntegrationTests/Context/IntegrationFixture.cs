using System;
using Hermes;
using Hermes.Packets;

namespace IntegrationTests.Context
{
	public class IntegrationFixture : IDisposable
	{
		private Server server;

		public IntegrationFixture ()
		{
			InitializeServer ();
		}

		public ProtocolConfiguration Configuration
		{
			get
			{
				return new ProtocolConfiguration {
					BufferSize = 128 * 1024,
					Port = Protocol.DefaultNonSecurePort,
					KeepAliveSecs = 0,
					WaitingTimeoutSecs = 10,
					MaximumQualityOfService = QualityOfService.ExactlyOnce
				};
			}
		}

		public Server Server { get { return this.server; } }

		public void Dispose ()
		{
			this.server.Stop ();
		}

		private void InitializeServer()
		{
			var initializer = new ServerInitializer ();
			
			this.server = initializer.Initialize (this.Configuration);

			this.server.Start ();
		}
	}
}
