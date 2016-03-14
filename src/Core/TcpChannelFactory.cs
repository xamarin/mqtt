using System.Net.Sockets;
using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Exceptions;

namespace System.Net.Mqtt
{
	internal class TcpChannelFactory : IChannelFactory
	{
		static readonly ITracer tracer = Tracer.Get<TcpChannelFactory> ();

		readonly string hostAddress;
		readonly ProtocolConfiguration configuration;

		public TcpChannelFactory (string hostAddress, ProtocolConfiguration configuration)
		{
			this.hostAddress = hostAddress;
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

			return new TcpChannel (tcpClient, new PacketBuffer (), configuration);
		}
	}
}
