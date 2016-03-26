using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Exceptions;
using System.Net.Sockets;

namespace System.Net.Mqtt.Client
{
	internal class TcpChannelFactory : IChannelFactory
	{
		readonly ITracer tracer;
		readonly string hostAddress;
		readonly ITracerManager tracerManager;
		readonly ProtocolConfiguration configuration;

		public TcpChannelFactory (string hostAddress, ITracerManager tracerManager, ProtocolConfiguration configuration)
		{
			tracer = tracerManager.Get<TcpChannelFactory> ();
			this.hostAddress = hostAddress;
			this.tracerManager = tracerManager;
			this.configuration = configuration;
		}

		/// <exception cref="MqttException">ProtocolException</exception>
		public IChannel<byte[]> Create ()
		{
			var tcpClient = new TcpClient ();

			try {
				tcpClient.Connect (hostAddress, configuration.Port);
			} catch (SocketException socketEx) {
				var message = string.Format(Properties.Resources.TcpChannelFactory_TcpClient_Failed, hostAddress, configuration.Port);

				tracer.Error (socketEx, message);

				throw new MqttException (message, socketEx);
			}

			return new TcpChannel (tcpClient, new PacketBuffer (), tracerManager, configuration);
		}
	}
}
