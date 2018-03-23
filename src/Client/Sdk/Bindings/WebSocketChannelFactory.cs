using System.Diagnostics;
using System.Net.WebSockets;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Sdk.Bindings
{
	internal class WebSocketChannelFactory : IMqttChannelFactory
	{
		static readonly ITracer tracer = Tracer.Get<WebSocketChannelFactory> ();

		readonly string hostAddress;
		readonly MqttConfiguration configuration;

		public WebSocketChannelFactory (string hostAddress, MqttConfiguration configuration)
		{
			this.hostAddress = hostAddress.StartsWith ("ws://") ? hostAddress : $"ws://{hostAddress}";
			this.configuration = configuration;
		}

		public async Task<IMqttChannel<byte[]>> CreateAsync ()
		{
			var webSocketClient = new ClientWebSocket ();

			try {
				var connectTask = webSocketClient.ConnectAsync (new Uri (hostAddress), CancellationToken.None);
				var timeoutTask = Task.Delay (TimeSpan.FromSeconds (configuration.ConnectionTimeoutSecs));
				var resultTask = await Task
					.WhenAny (connectTask, timeoutTask)
					.ConfigureAwait (continueOnCapturedContext: false);

				if (resultTask == timeoutTask)
					throw new TimeoutException ();

				if (resultTask.IsFaulted)
					ExceptionDispatchInfo.Capture (resultTask.Exception.InnerException).Throw ();

				return new WebSocketChannel (webSocketClient, new PacketBuffer (), configuration);
			} catch (TimeoutException timeoutEx) {
				try {
					// Just in case the connection is a little late,
					// dispose the webSocketClient. This may throw an exception,
					// which we should just eat.
					webSocketClient.Dispose ();
				}
				catch { }

				var message = string.Format (Properties.Resources.WebSocketChannelFactory_WebSocketClient_Failed, hostAddress);

				tracer.Error (timeoutEx, message);

				throw new MqttException (message, timeoutEx);
			} catch (Exception ex) {
				var message = string.Format (Properties.Resources.WebSocketChannelFactory_WebSocketClient_Failed, hostAddress);

				tracer.Error (ex, message);

				throw new MqttException (message, ex);
			}
		}
	}
}
