using System.Diagnostics;
using System.Net.Mqtt.Exceptions;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Bindings
{
    internal class TcpChannelFactory : IMqttChannelFactory
	{
		static readonly ITracer tracer = Tracer.Get<TcpChannelFactory> ();

		readonly string hostAddress;
		readonly MqttConfiguration configuration;

		public TcpChannelFactory (string hostAddress, MqttConfiguration configuration)
		{
			this.hostAddress = hostAddress;
			this.configuration = configuration;
		}

		/// <exception cref="MqttException">ProtocolException</exception>
		public async Task<IMqttChannel<byte[]>> CreateAsync ()
		{
			var tcpClient = new TcpClient ();

			try {
				await tcpClient
                    .ConnectAsync (hostAddress, configuration.Port)
                    .ConfigureAwait (continueOnCapturedContext: false);
			} catch (SocketException socketEx) {
				var message = string.Format (Resources.TcpChannelFactory_TcpClient_Failed, hostAddress, configuration.Port);

				tracer.Error (socketEx, message);

				throw new MqttException (message, socketEx);
			}

			return new TcpChannel (tcpClient, new PacketBuffer (), configuration);
		}
	}
}
