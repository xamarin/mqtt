using System.Diagnostics;
using System.Net.Mqtt.Bindings;
using System.Net.Mqtt.Exceptions;
using System.Net.Sockets;
using System.Reactive.Linq;

namespace System.Net.Mqtt.Server.Bindings
{
    internal class TcpChannelProvider : IMqttChannelProvider
	{
		static readonly ITracer tracer = Tracer.Get<TcpChannelProvider> ();

		readonly MqttConfiguration configuration;
		readonly Lazy<TcpListener> listener;
		bool disposed;

		public TcpChannelProvider (MqttConfiguration configuration)
		{
			this.configuration = configuration;
			listener = new Lazy<TcpListener> (() => {
				var tcpListener = new TcpListener(IPAddress.Any, this.configuration.Port);

				try {
					tcpListener.Start ();
				} catch (SocketException socketEx) {
					tracer.Error (socketEx, Resources.TcpChannelProvider_TcpListener_Failed);

					throw new MqttException (Resources.TcpChannelProvider_TcpListener_Failed, socketEx);
				}

				return tcpListener;
			});
		}

		/// <exception cref="MqttException">ProtocolException</exception>
		public IObservable<IMqttChannel<byte[]>> GetChannels ()
		{
			if (disposed) {
				throw new ObjectDisposedException (GetType ().FullName);
			}

			return Observable
				.FromAsync (listener.Value.AcceptTcpClientAsync)
				.Repeat ()
				.Select (client => new TcpChannel (client, new PacketBuffer (), configuration));
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposed) return;

			if (disposing) {
				listener.Value.Stop ();
				disposed = true;
			}
		}
	}
}
