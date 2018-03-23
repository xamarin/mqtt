using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Sdk.Bindings
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

		public async Task<IMqttChannel<byte[]>> CreateAsync ()
		{
			var tcpClient = new TcpClient ();

			try {
				var connectTask = tcpClient.ConnectAsync (hostAddress, configuration.Port);
				var timeoutTask = Task.Delay (TimeSpan.FromSeconds (configuration.ConnectionTimeoutSecs));
				var resultTask = await Task
					.WhenAny (connectTask, timeoutTask)
					.ConfigureAwait (continueOnCapturedContext: false);

				if (resultTask == timeoutTask)
					throw new TimeoutException ();

				if (resultTask.IsFaulted)
					ExceptionDispatchInfo.Capture (resultTask.Exception.InnerException).Throw();

				return new TcpChannel (tcpClient, new PacketBuffer (), configuration);
			} catch (SocketException socketEx) {
				var message = string.Format (Properties.Resources.TcpChannelFactory_TcpClient_Failed, hostAddress, configuration.Port);

				tracer.Error (socketEx, message);

				throw new MqttException (message, socketEx);
			} catch (TimeoutException timeoutEx) {
				try {
					// Just in case the connection is a little late,
					// dispose the tcpClient. This may throw an exception,
					// which we should just eat.
					tcpClient.Dispose ();
				} catch {}

				var message = string.Format (Properties.Resources.TcpChannelFactory_TcpClient_Failed, hostAddress, configuration.Port);

				tracer.Error (timeoutEx, message);

				throw new MqttException (message, timeoutEx);
			}
		}
	}
}
