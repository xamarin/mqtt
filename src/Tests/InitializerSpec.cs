using System;
using System.Net;
using System.Net.Mqtt;
using System.Net.Mqtt.Client;
using System.Net.Mqtt.Packets;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;
using Server = System.Net.Mqtt.Server;

namespace Tests
{
    public class InitializerSpec
	{
		[Fact]
		public void when_creating_protocol_configuration_then_default_values_are_set()
		{
			var configuration = new ProtocolConfiguration ();

			Assert.Equal (Protocol.DefaultNonSecurePort, configuration.Port);
			Assert.Equal (8192, configuration.BufferSize);
			Assert.Equal (QualityOfService.AtMostOnce, configuration.MaximumQualityOfService);
			Assert.Equal (0, configuration.KeepAliveSecs);
			Assert.Equal (5, configuration.WaitingTimeoutSecs);
			Assert.Equal (true, configuration.AllowWildcardsInTopicFilters);
		}

		[Fact]
		public async Task when_initializing_server_then_succeeds()
		{
			var configuration = new ProtocolConfiguration {
				BufferSize = 131072,
				Port = Protocol.DefaultNonSecurePort
			};
			var binding = new TcpBinding ();
			var initializer = new Server.ServerFactory (binding);
			var server = await initializer.CreateAsync (configuration);

			Assert.NotNull (server);

			server.Stop ();
		}

		[Fact]
		public async Task when_initializing_client_then_succeeds()
		{
			var port = new Random().Next(IPEndPoint.MinPort, IPEndPoint.MaxPort);
			var listener = new TcpListener(IPAddress.Loopback, port);

			listener.Start ();

			var configuration = new ProtocolConfiguration {
				BufferSize = 131072,
				Port = port
			};
			var binding = new TcpBinding ();
			var initializer = new ClientFactory (IPAddress.Loopback.ToString(), binding);
			var client = await initializer.CreateAsync (configuration);

			Assert.NotNull (client);

			listener.Stop ();
		}
	}
}
