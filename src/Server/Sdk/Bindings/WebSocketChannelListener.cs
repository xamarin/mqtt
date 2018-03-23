using System.Diagnostics;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace System.Net.Mqtt.Sdk.Bindings
{
	/// <summary>
	/// Represents a listener for incoming channel connections on top of Web Sockets
	/// The connections are performed by MQTT Clients through <see cref="IMqttChannel{T}" /> 
	/// and the listener accepts and establishes the connection on the Server side
	/// </summary>
	public class WebSocketChannelListener : IMqttChannelListener
	{
		static readonly ITracer tracer = Tracer.Get<WebSocketChannelListener> ();
		static readonly Subject<WebSocket> listener = new Subject<WebSocket> ();

		readonly MqttConfiguration configuration;
		bool disposed;

		internal WebSocketChannelListener (MqttConfiguration configuration)
		{
			this.configuration = configuration;
		}

		/// <summary>
		/// Accepts an incoming client request as a Web Socket
		/// </summary>
		/// <param name="webSocket">The Web Socket instance to accept</param>
		public static void AcceptWebSocketClient (WebSocket webSocket) => listener.OnNext (webSocket);

		/// <summary>
		/// Provides the stream of incoming channels on top of Web Sockets
		/// </summary>
		/// <returns>An observable sequence of <see cref="IMqttChannel{T}"/> of byte[]</returns>
		public IObservable<IMqttChannel<byte[]>> GetChannelStream ()
		{
			if (disposed) {
				throw new ObjectDisposedException (GetType ().FullName);
			}

			return Observable.Create<IMqttChannel<byte[]>> (observer => {
				return listener.Subscribe (webSocket => {
					var channel = new WebSocketChannel (webSocket, new PacketBuffer (), configuration);

					observer.OnNext (channel);
				});
			});
		}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public void Dispose ()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		protected virtual void Dispose (bool disposing)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
		{
			if (disposed) return;

			if (disposing) {
				listener.Dispose ();
				disposed = true;
			}
		}
	}
}
