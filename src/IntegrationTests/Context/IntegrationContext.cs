using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Mqtt;
using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Packets;
using System.Linq;
using System.Net.Sockets;
using Server = System.Net.Mqtt.Server;
using System.Net.Mqtt.Client;
using System.Net.Mqtt.Exceptions;

namespace IntegrationTests.Context
{
	public abstract class IntegrationContext
	{
		static readonly DiagnosticsTracerManager tracerManager;
		static readonly ConcurrentBag<int> usedPorts;
		static readonly Random random = new Random ();
		static readonly object lockObject = new object ();

		protected readonly ushort keepAliveSecs;

		static IntegrationContext()
		{
			tracerManager = new DiagnosticsTracerManager ();

			tracerManager.AddListener ("System.Net.Mqtt", new TestTracerListener ());
			tracerManager.SetTracingLevel ("System.Net.Mqtt", SourceLevels.All);

			usedPorts = new ConcurrentBag<int> ();
		}

		public IntegrationContext (ushort keepAliveSecs = 0)
		{
			this.keepAliveSecs = keepAliveSecs;
		}

		protected ProtocolConfiguration Configuration { get; private set; }

		protected Server.Server GetServer(Server.IAuthenticationProvider authenticationProvider = null)
		{
			try {
				LoadConfiguration ();

				var binding = new Server.TcpBinding ();
				var initializer = new Server.ServerFactory (binding, authenticationProvider);
				var server = initializer.Create (Configuration);

				server.Start ();

				return server;
			} catch (MqttException protocolEx) {
				if (protocolEx.InnerException is SocketException) {
					return GetServer ();
				} else {
					throw;
				}
			}
		}

		protected virtual Client GetClient()
		{
			var binding = new TcpBinding ();
			var initializer = new ClientFactory (IPAddress.Loopback.ToString(), binding);

			if (Configuration == null) {
				LoadConfiguration ();
			}

			return initializer.Create (Configuration);
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

		void LoadConfiguration()
		{
			lock (lockObject) {
				Configuration = new ProtocolConfiguration {
					BufferSize = 128 * 1024,
					Port = GetPort (),
					KeepAliveSecs = keepAliveSecs,
					WaitingTimeoutSecs = 2,
					MaximumQualityOfService = QualityOfService.ExactlyOnce
				};
			}
		}

		static int GetPort()
		{
			var port = random.Next (minValue: 40000, maxValue: 65535);

			if(usedPorts.Any(p => p == port)) {
				port = GetPort ();
			} else {
				usedPorts.Add (port);
			}

			return port;
		}
	}
}
