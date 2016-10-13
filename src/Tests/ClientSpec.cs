using Xunit;

namespace Tests
{
	public class ClientSpec
	{
		//[Fact]
		//public void when_creating_client_then_it_is_not_connected()
		//{
		//	var address = "192.168.1.1";
		//	var configuration = new Mock<ProtocolConfiguration>();
		//	var socketFactory = new Mock<ISocketFactory>();

		//	var socket = new Mock<IBufferedChannel<byte>>();
		//	var receiver = new Subject<byte> ();

		//	socket.Setup (s => s.Receiver).Returns (receiver);
		//	socketFactory.Setup (f => f.CreateSocket (It.IsAny<string> ())).Returns (socket.Object);

		//	var factory = new ClientFactory (address, configuration.Object, socketFactory.Object);
		//	var client = factory.CreateClient ();

		//	socketFactory.Verify (f => f.CreateSocket (It.Is<string> (s => s == address)));
		//	Assert.NotNull (client);
		//	Assert.False (client.IsConnected);
		//}

		[Fact]
		public void when_starting_connection_then_valid_client_is_created()
		{

		}
	}
}
