using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Mqtt;
using System.Net.Mqtt.Packets;
using System.Linq;
using System.Net.Sockets;
using System.Net.Mqtt.Exceptions;
using System.Net.Mqtt.Server;
using System.Net.Mqtt.Server.Bindings;
using System.Threading.Tasks;

namespace IntegrationTests.Context
{
	public abstract class IntegrationContext
	{
		//static readonly DiagnosticsTracerManager tracerManager;
		static readonly ConcurrentBag<int> usedPorts;
		static readonly Random random = new Random ();
		static readonly object lockObject = new object ();

		protected readonly ushort keepAliveSecs;

		static IntegrationContext()
		{
			//tracerManager = new DiagnosticsTracerManager ();

			//tracerManager.AddListener ("System.Net.Mqtt", new TestTracerListener ());
			//tracerManager.SetTracingLevel ("System.Net.Mqtt", SourceLevels.All);

			usedPorts = new ConcurrentBag<int> ();
		}

		public IntegrationContext (ushort keepAliveSecs = 0)
		{
			this.keepAliveSecs = keepAliveSecs;
		}

		protected MqttConfiguration Configuration { get; private set; }

		protected async Task<IMqttServer> GetServerAsync (IMqttAuthenticationProvider authenticationProvider = null)
		{
			try {
				LoadConfiguration ();

				var binding = new TcpBinding ();
				var initializer = new MqttServerFactory (binding, authenticationProvider);
				var server = await initializer.CreateAsync (Configuration);

				server.Start ();

				return server;
			} catch (MqttException protocolEx) {
				if (protocolEx.InnerException is SocketException) {
					return await GetServerAsync ();
				} else {
					throw;
				}
			}
		}

		protected virtual async Task<IMqttClient> GetClientAsync ()
		{
			var binding = new TcpBinding ();
			var initializer = new MqttClientFactory (IPAddress.Loopback.ToString(), binding);

			if (Configuration == null) {
				LoadConfiguration ();
			}

			return await initializer.CreateAsync (Configuration);
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
				Configuration = new MqttConfiguration {
					BufferSize = 128 * 1024,
					Port = GetPort (),
					KeepAliveSecs = keepAliveSecs,
					WaitingTimeoutSecs = 2,
					MaximumQualityOfService = MqttQualityOfService.ExactlyOnce
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
