using System.Net;
using System.Net.Sockets;
using Hermes;
using Hermes.Packets;
using Xunit;

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
			Assert.Equal (3, configuration.QualityOfServiceAckRetries);
			Assert.Equal (0, configuration.KeepAliveSecs);
			Assert.Equal (5, configuration.WaitingTimeoutSecs);
			Assert.Equal (true, configuration.AllowWildcardsInTopicFilters);
		}

		[Fact]
		public void when_initializing_server_then_succeeds()
		{
			var configuration = new ProtocolConfiguration {
				BufferSize = 131072,
				Port = Protocol.DefaultNonSecurePort
			};
			var initializer = new ServerInitializer ();
			var server = initializer.Initialize (configuration);

			Assert.NotNull (server);

			server.Stop ();
		}

		[Fact]
		public void when_initializing_client_then_succeeds()
		{
			var listener = new TcpListener(IPAddress.Loopback, Protocol.DefaultNonSecurePort);

			listener.Start ();

			var configuration = new ProtocolConfiguration {
				BufferSize = 131072,
				Port = Protocol.DefaultNonSecurePort
			};
			var initializer = new ClientInitializer (IPAddress.Loopback.ToString());
			var client = initializer.Initialize (configuration);

			Assert.NotNull (client);

			listener.Stop ();
		}
	}
}
