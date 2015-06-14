using System.Net.Sockets;
using System.Net.Mqtt.Diagnostics;

namespace System.Net.Mqtt
{
	public class TcpChannelFactory : IChannelFactory
	{
		static readonly ITracer tracer = Tracer.Get<TcpChannelFactory> ();

		readonly string hostAddress;
		readonly ProtocolConfiguration configuration;

		public TcpChannelFactory (string hostAddress, ProtocolConfiguration configuration)
		{
			this.hostAddress = hostAddress;
			this.configuration = configuration;
		}

		/// <exception cref="ProtocolException">ProtocolException</exception>
		public IChannel<byte[]> Create ()
		{
			var tcpClient = new TcpClient ();

			try {
				tcpClient.Connect (this.hostAddress, this.configuration.Port);
			} catch (SocketException socketEx) {
				var message = string.Format(Properties.Resources.TcpChannelFactory_TcpClient_Failed, this.hostAddress, configuration.Port);

				tracer.Error (socketEx, message);

				throw new ProtocolException(message, socketEx);
			}

			return new TcpChannel(tcpClient, new PacketBuffer(), this.configuration);
		}
	}
}
