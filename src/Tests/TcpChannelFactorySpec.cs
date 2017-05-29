using System;
using System.Net;
using System.Net.Mqtt;
using System.Net.Mqtt.Sdk.Bindings;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
	public class TcpChannelFactorySpec
	{
		[Fact]
		public async Task when_creating_channel_then_succeeds()
		{
			var configuration = new MqttConfiguration { ConnectionTimeoutSecs = 2 };
			var listener = new TcpListener (IPAddress.Loopback, configuration.Port);

			listener.Start ();

			var factory = new TcpChannelFactory (IPAddress.Loopback.ToString (), configuration);
			var channel = await factory.CreateAsync ();

			Assert.NotNull (channel);
			Assert.True (channel.IsConnected);

			listener.Stop ();
		}

		[Fact]
		public void when_creating_channel_with_invalid_address_then_fails()
		{
			var configuration = new MqttConfiguration { ConnectionTimeoutSecs = 2 };
			var factory = new TcpChannelFactory (IPAddress.Loopback.ToString (), configuration);

			var ex = Assert.Throws<AggregateException> (() => factory.CreateAsync ().Result);

			Assert.NotNull (ex);
			Assert.NotNull (ex.InnerException);
			Assert.True (ex.InnerException is MqttException);
			Assert.NotNull (ex.InnerException.InnerException);
			Assert.True (ex.InnerException.InnerException is SocketException);
		}
	}
}
