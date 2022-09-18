using Moq;
using System;
using System.Net.Mqtt;
using System.Net.Mqtt.Sdk;
using System.Net.Mqtt.Sdk.Bindings;
using System.Net.Mqtt.Sdk.Flows;
using System.Net.Mqtt.Sdk.Packets;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
	public class ServerSpec
	{
		[Fact]
		public void when_server_does_not_start_then_connections_are_ignored()
		{
			var configuration = Mock.Of<MqttConfiguration>(c => c.WaitTimeoutSecs == 60);
			var channelFactory = new Mock<IMqttChannelFactory>();

			channelFactory
				.Setup(f => f.CreateAsync())
				.ReturnsAsync(Mock.Of<IMqttChannel<byte[]>>());

			var sockets = new Subject<IMqttChannel<byte[]>>();
			var channelProvider = new Mock<IMqttChannelListener>();

			channelProvider
				.Setup(p => p.GetChannelStream())
				.Returns(sockets);

			var binding = new Mock<IMqttServerBinding>();

			binding
				.Setup(b => b.GetChannelFactory(It.IsAny<string>(), It.Is<MqttConfiguration>(c => c == configuration)))
				.Returns(channelFactory.Object);

			binding
				.Setup(b => b.GetChannelListener(It.Is<MqttConfiguration>(c => c == configuration)))
				.Returns(channelProvider.Object);

			var privateSockets = new Subject<IMqttChannel<byte[]>>();
			var privateChannelProvider = new Mock<IMqttChannelListener>();

			privateChannelProvider
				.Setup(p => p.GetChannelStream())
				.Returns(privateSockets);

			var privateBinding = new Mock<IServerPrivateBinding>();

			privateBinding
				.Setup(b => b.GetChannelListener(It.Is<MqttConfiguration>(c => c == configuration)))
				.Returns(privateChannelProvider.Object);

			var packets = new Subject<IPacket>();
			var packetChannel = new Mock<IMqttChannel<IPacket>>();
			var factory = Mock.Of<IPacketChannelFactory>(x => x.Create(It.IsAny<IMqttChannel<byte[]>>()) == packetChannel.Object);

			packetChannel
				.Setup(c => c.IsConnected)
				.Returns(true);
			packetChannel
				.Setup(c => c.SenderStream)
				.Returns(new Subject<IPacket>());
			packetChannel
				.Setup(c => c.ReceiverStream)
				.Returns(packets);

			var flowProvider = Mock.Of<IProtocolFlowProvider>();
			var connectionProvider = new Mock<IConnectionProvider>();

			var server = new MqttServerImpl(privateBinding.Object, factory, flowProvider, connectionProvider.Object, Mock.Of<ISubject<MqttUndeliveredMessage>>(), configuration, binding.Object);

			sockets.OnNext(Mock.Of<IMqttChannel<byte[]>>(x => x.ReceiverStream == new Subject<byte[]>()));
			sockets.OnNext(Mock.Of<IMqttChannel<byte[]>>(x => x.ReceiverStream == new Subject<byte[]>()));
			sockets.OnNext(Mock.Of<IMqttChannel<byte[]>>(x => x.ReceiverStream == new Subject<byte[]>()));

			Assert.Equal(0, server.ActiveConnections);
		}

		[Fact]
		public void when_connection_established_then_active_connections_increases()
		{
			var configuration = Mock.Of<MqttConfiguration>(c => c.WaitTimeoutSecs == 60);
			var channelFactory = new Mock<IMqttChannelFactory>();

			channelFactory
				.Setup(f => f.CreateAsync())
				.ReturnsAsync(Mock.Of<IMqttChannel<byte[]>>());

			var sockets = new Subject<IMqttChannel<byte[]>>();
			var channelProvider = new Mock<IMqttChannelListener>();

			channelProvider
				.Setup(p => p.GetChannelStream())
				.Returns(sockets);

			var binding = new Mock<IMqttServerBinding>();

			binding
				.Setup(b => b.GetChannelFactory(It.IsAny<string>(), It.Is<MqttConfiguration>(c => c == configuration)))
				.Returns(channelFactory.Object);

			binding
				.Setup(b => b.GetChannelListener(It.Is<MqttConfiguration>(c => c == configuration)))
				.Returns(channelProvider.Object);

			var privateSockets = new Subject<IMqttChannel<byte[]>>();
			var privateChannelProvider = new Mock<IMqttChannelListener>();

			privateChannelProvider
				.Setup(p => p.GetChannelStream())
				.Returns(privateSockets);

			var privateBinding = new Mock<IServerPrivateBinding>();

			privateBinding
				.Setup(b => b.GetChannelListener(It.Is<MqttConfiguration>(c => c == configuration)))
				.Returns(privateChannelProvider.Object);

			var packets = new Subject<IPacket>();
			var packetChannel = new Mock<IMqttChannel<IPacket>>();
			var factory = Mock.Of<IPacketChannelFactory>(x => x.Create(It.IsAny<IMqttChannel<byte[]>>()) == packetChannel.Object);

			packetChannel
				.Setup(c => c.IsConnected)
				.Returns(true);
			packetChannel
				.Setup(c => c.SenderStream)
				.Returns(new Subject<IPacket>());
			packetChannel
				.Setup(c => c.ReceiverStream)
				.Returns(packets);

			var flowProvider = Mock.Of<IProtocolFlowProvider>();
			var connectionProvider = new Mock<IConnectionProvider>();

			var server = new MqttServerImpl(privateBinding.Object, factory, flowProvider, connectionProvider.Object, Mock.Of<ISubject<MqttUndeliveredMessage>>(), configuration, binding.Object);

			server.Start();

			sockets.OnNext(Mock.Of<IMqttChannel<byte[]>>(x => x.ReceiverStream == new Subject<byte[]>()));
			sockets.OnNext(Mock.Of<IMqttChannel<byte[]>>(x => x.ReceiverStream == new Subject<byte[]>()));
			sockets.OnNext(Mock.Of<IMqttChannel<byte[]>>(x => x.ReceiverStream == new Subject<byte[]>()));

			Assert.Equal(3, server.ActiveConnections);
		}

		[Fact]
		public void when_using_in_memory_server_and_connection_established_then_active_connections_increases()
		{
			var configuration = Mock.Of<MqttConfiguration>(c => c.WaitTimeoutSecs == 60);

			var privateSockets = new Subject<IMqttChannel<byte[]>>();
			var privateChannelProvider = new Mock<IMqttChannelListener>();

			privateChannelProvider
				.Setup(p => p.GetChannelStream())
				.Returns(privateSockets);

			var privateBinding = new Mock<IServerPrivateBinding>();

			privateBinding
				.Setup(b => b.GetChannelListener(It.Is<MqttConfiguration>(c => c == configuration)))
				.Returns(privateChannelProvider.Object);

			var packets = new Subject<IPacket>();
			var packetChannel = new Mock<IMqttChannel<IPacket>>();
			var factory = Mock.Of<IPacketChannelFactory>(x => x.Create(It.IsAny<IMqttChannel<byte[]>>()) == packetChannel.Object);

			packetChannel
				.Setup(c => c.IsConnected)
				.Returns(true);
			packetChannel
				.Setup(c => c.SenderStream)
				.Returns(new Subject<IPacket>());
			packetChannel
				.Setup(c => c.ReceiverStream)
				.Returns(packets);

			var flowProvider = Mock.Of<IProtocolFlowProvider>();
			var connectionProvider = new Mock<IConnectionProvider>();

			var server = new MqttServerImpl(privateBinding.Object, factory, flowProvider, connectionProvider.Object, 
				Mock.Of<ISubject<MqttUndeliveredMessage>>(), configuration, binding: null);

			server.Start();

			privateSockets.OnNext(Mock.Of<IMqttChannel<byte[]>>(x => x.ReceiverStream == new Subject<byte[]>()));
			privateSockets.OnNext(Mock.Of<IMqttChannel<byte[]>>(x => x.ReceiverStream == new Subject<byte[]>()));
			privateSockets.OnNext(Mock.Of<IMqttChannel<byte[]>>(x => x.ReceiverStream == new Subject<byte[]>()));

			Assert.Equal(3, server.ActiveConnections);
		}

		[Fact]
		public void when_server_closed_then_pending_connection_is_closed()
		{
			var configuration = Mock.Of<MqttConfiguration>(c => c.WaitTimeoutSecs == 60);
			var channelFactory = new Mock<IMqttChannelFactory>();

			channelFactory
				.Setup(f => f.CreateAsync())
				.ReturnsAsync(Mock.Of<IMqttChannel<byte[]>>());

			var sockets = new Subject<IMqttChannel<byte[]>>();
			var channelProvider = new Mock<IMqttChannelListener>();

			channelProvider
				.Setup(p => p.GetChannelStream())
				.Returns(sockets);

			var binding = new Mock<IMqttServerBinding>();

			binding
				.Setup(b => b.GetChannelFactory(It.IsAny<string>(), It.Is<MqttConfiguration>(c => c == configuration)))
				.Returns(channelFactory.Object);

			binding
				.Setup(b => b.GetChannelListener(It.Is<MqttConfiguration>(c => c == configuration)))
				.Returns(channelProvider.Object);

			var privateSockets = new Subject<IMqttChannel<byte[]>>();
			var privateChannelProvider = new Mock<IMqttChannelListener>();

			privateChannelProvider
				.Setup(p => p.GetChannelStream())
				.Returns(privateSockets);

			var privateBinding = new Mock<IServerPrivateBinding>();

			privateBinding
				.Setup(b => b.GetChannelListener(It.Is<MqttConfiguration>(c => c == configuration)))
				.Returns(privateChannelProvider.Object);

			var packetChannel = new Mock<IMqttChannel<IPacket>>();

			packetChannel
				.Setup(c => c.IsConnected)
				.Returns(true);
			packetChannel
				.Setup(c => c.SenderStream)
				.Returns(new Subject<IPacket>());
			packetChannel
				.Setup(c => c.ReceiverStream)
				.Returns(new Subject<IPacket>());

			var flowProvider = Mock.Of<IProtocolFlowProvider>();
			var connectionProvider = new Mock<IConnectionProvider>();

			var server = new MqttServerImpl(privateBinding.Object, Mock.Of<IPacketChannelFactory>(x => x.Create(It.IsAny<IMqttChannel<byte[]>>()) == packetChannel.Object),
				flowProvider, connectionProvider.Object, Mock.Of<ISubject<MqttUndeliveredMessage>>(), configuration, binding.Object);

			server.Start();

			var socket = new Mock<IMqttChannel<byte[]>>();

			sockets.OnNext(socket.Object);

			server.Stop();

			packetChannel.Verify(x => x.CloseAsync());
		}

		[Fact]
		public async Task when_receiver_error_then_closes_connection()
		{
			var configuration = Mock.Of<MqttConfiguration>(c => c.WaitTimeoutSecs == 60);
			var channelFactory = new Mock<IMqttChannelFactory>();

			channelFactory
				.Setup(f => f.CreateAsync())
				.ReturnsAsync(Mock.Of<IMqttChannel<byte[]>>());

			var sockets = new Subject<IMqttChannel<byte[]>>();
			var channelProvider = new Mock<IMqttChannelListener>();

			channelProvider
				.Setup(p => p.GetChannelStream())
				.Returns(sockets);

			var binding = new Mock<IMqttServerBinding>();

			binding
				.Setup(b => b.GetChannelFactory(It.IsAny<string>(), It.Is<MqttConfiguration>(c => c == configuration)))
				.Returns(channelFactory.Object);

			binding
				.Setup(b => b.GetChannelListener(It.Is<MqttConfiguration>(c => c == configuration)))
				.Returns(channelProvider.Object);

			var privateSockets = new Subject<IMqttChannel<byte[]>>();
			var privateChannelProvider = new Mock<IMqttChannelListener>();

			privateChannelProvider
				.Setup(p => p.GetChannelStream())
				.Returns(privateSockets);

			var privateBinding = new Mock<IServerPrivateBinding>();

			privateBinding
				.Setup(b => b.GetChannelListener(It.Is<MqttConfiguration>(c => c == configuration)))
				.Returns(privateChannelProvider.Object);

			var packets = new Subject<IPacket>();

			var packetChannel = new Mock<IMqttChannel<IPacket>>();
			var factory = new Mock<IPacketChannelFactory>();

			packetChannel
				.Setup(c => c.IsConnected)
				.Returns(true);
			packetChannel
				.Setup(c => c.SenderStream)
				.Returns(new Subject<IPacket>());
			packetChannel
				.Setup(c => c.ReceiverStream)
				.Returns(packets);

			factory.Setup(x => x.Create(It.IsAny<IMqttChannel<byte[]>>()))
				.Returns(packetChannel.Object);

			var flowProvider = Mock.Of<IProtocolFlowProvider>();
			var connectionProvider = new Mock<IConnectionProvider>();

			var server = new MqttServerImpl(privateBinding.Object, factory.Object, flowProvider, connectionProvider.Object, Mock.Of<ISubject<MqttUndeliveredMessage>>(), configuration, binding.Object);
			var receiver = new Subject<byte[]>();
			var socket = new Mock<IMqttChannel<byte[]>>();

			socket.Setup(x => x.ReceiverStream).Returns(receiver);

			server.Start();

			sockets.OnNext(socket.Object);

			try
			{
				packets.OnError(new Exception("Protocol exception"));
			}
			catch (Exception)
			{
			}

			await Task.Delay(TimeSpan.FromMilliseconds(1000));

			packetChannel.Verify(x => x.CloseAsync());
		}
	}
}
