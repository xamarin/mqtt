using System.Net;
using System.Net.Sockets;
using Hermes;
using Xunit;

namespace Tests
{
	public class InitializerSpec
	{
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
