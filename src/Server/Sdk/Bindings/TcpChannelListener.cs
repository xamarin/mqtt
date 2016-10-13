using System.Diagnostics;
using System.Net.Sockets;
using System.Reactive.Linq;

namespace System.Net.Mqtt.Sdk.Bindings
{
	internal class TcpChannelListener : IMqttChannelListener
	{
		static readonly ITracer tracer = Tracer.Get<TcpChannelListener> ();

		readonly MqttConfiguration configuration;
		readonly Lazy<TcpListener> listener;
		bool disposed;

		public TcpChannelListener (MqttConfiguration configuration)
		{
			this.configuration = configuration;
			listener = new Lazy<TcpListener> (() => {
				var tcpListener = new TcpListener (IPAddress.Any, this.configuration.Port);

				try {
					tcpListener.Start ();
				} catch (SocketException socketEx) {
					tracer.Error (socketEx, Properties.Resources.TcpChannelProvider_TcpListener_Failed);

					throw new MqttException (Properties.Resources.TcpChannelProvider_TcpListener_Failed, socketEx);
				}

				return tcpListener;
			});
		}

        public IObservable<IMqttChannel<byte[]>> GetChannelStream ()
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
