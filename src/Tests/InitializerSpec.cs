using System;
using System.Net;
using System.Net.Mqtt;
using System.Net.Mqtt.Sdk;
using System.Net.Mqtt.Sdk.Bindings;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
	public class InitializerSpec
	{
		[Fact]
		public void when_creating_protocol_configuration_then_default_values_are_set()
		{
			var configuration = new MqttConfiguration ();

			Assert.Equal (MqttProtocol.DefaultNonSecurePort, configuration.Port);
			Assert.Equal (8192, configuration.BufferSize);
			Assert.Equal (MqttQualityOfService.AtMostOnce, configuration.MaximumQualityOfService);
			Assert.Equal (0, configuration.KeepAliveSecs);
			Assert.Equal (5, configuration.WaitTimeoutSecs);
			Assert.True (configuration.AllowWildcardsInTopicFilters);
		}

		[Fact]
		public void when_initializing_server_then_succeeds()
		{
			var configuration = new MqttConfiguration {
				BufferSize = 131072,
				Port = MqttProtocol.DefaultNonSecurePort
			};
			var binding = new ServerTcpBinding ();
			var initializer = new MqttServerFactory (binding);
			var server = initializer.CreateServer (configuration);

			Assert.NotNull (server);

			server.Stop ();
		}

		[Fact]
		public async Task when_initializing_client_then_succeeds()
		{
			var port = new Random().Next(IPEndPoint.MinPort, IPEndPoint.MaxPort);
			var listener = new TcpListener(IPAddress.Loopback, port);

			listener.Start ();

			var configuration = new MqttConfiguration {
				BufferSize = 131072,
				Port = port
			};
			var binding = new TcpBinding ();
			var initializer = new MqttClientFactory (IPAddress.Loopback.ToString(), binding);
			var client = await initializer.CreateClientAsync (configuration);

			Assert.NotNull (client);

			listener.Stop ();
		}
	}
}
