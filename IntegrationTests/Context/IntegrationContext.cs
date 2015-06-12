using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using Hermes;
using Hermes.Diagnostics;
using Hermes.Packets;
using System.Linq;
using System.Net.Sockets;

namespace IntegrationTests.Context
{
	public abstract class IntegrationContext
	{
		private static readonly ConcurrentBag<int> usedPorts;
		private static Random random = new Random ();

		protected readonly ushort keepAliveSecs;
		
		static IntegrationContext()
		{
			Tracer.Manager.AddListener ("Hermes", new TestTracerListener ());
			Tracer.Manager.SetTracingLevel ("Hermes", SourceLevels.All);

			usedPorts = new ConcurrentBag<int> ();
		}

		public IntegrationContext (ushort keepAliveSecs = 0)
		{
			this.keepAliveSecs = keepAliveSecs;
		}

		protected ProtocolConfiguration Configuration { get; private set; }

		protected Server GetServer()
		{
			try {
				this.LoadConfiguration ();

				var initializer = new ServerInitializer ();
				var server = initializer.Initialize (this.Configuration);

				server.Start ();

				return server;
			} catch (ProtocolException protocolEx) {
				if (protocolEx.InnerException is SocketException) {
					return GetServer ();
				} else {
					throw;
				}
			}
		}

		protected virtual Client GetClient()
		{
			var initializer = new ClientInitializer (IPAddress.Loopback.ToString());

			if (this.Configuration == null) {
				this.LoadConfiguration ();
			}

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

		private void LoadConfiguration()
		{
			this.Configuration = new ProtocolConfiguration {
				BufferSize = 128 * 1024,
				Port = GetPort(),
				KeepAliveSecs = this.keepAliveSecs,
				WaitingTimeoutSecs = 2,
				MaximumQualityOfService = QualityOfService.ExactlyOnce
			};
		}

		private static int GetPort()
		{
			var port = random.Next (minValue: 50000, maxValue: 60000);

			if(usedPorts.Any(p => p == port)) {
				port = GetPort ();
			}

			return port;
		}
	}
}
