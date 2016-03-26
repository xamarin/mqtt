using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Exceptions;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
	internal class TcpChannelProvider : IChannelProvider
	{
		readonly ITracer tracer;
		readonly ITracerManager tracerManager;
		readonly ProtocolConfiguration configuration;
		readonly Lazy<TcpListener> listener;
		bool disposed;

		public TcpChannelProvider (ITracerManager tracerManager, ProtocolConfiguration configuration)
		{
			tracer = tracerManager.Get<TcpChannelProvider> ();
			this.tracerManager = tracerManager;
			this.configuration = configuration;
			listener = new Lazy<TcpListener> (() => {
				var tcpListener = new TcpListener(IPAddress.Any, this.configuration.Port);

				try {
					tcpListener.Start ();
				} catch (SocketException socketEx) {
					tracer.Error (socketEx, Properties.Resources.TcpChannelProvider_TcpListener_Failed);

					throw new MqttException (Properties.Resources.TcpChannelProvider_TcpListener_Failed, socketEx);
				}

				return tcpListener;
			});
		}

		/// <exception cref="MqttException">ProtocolException</exception>
		public IObservable<IChannel<byte[]>> GetChannels ()
		{
			if (disposed) {
				throw new ObjectDisposedException (GetType ().FullName);
			}

			return Observable
				.FromAsync (() => {
					return Task.Factory.FromAsync<TcpClient> (listener.Value.BeginAcceptTcpClient,
						listener.Value.EndAcceptTcpClient, TaskCreationOptions.AttachedToParent);
				})
				.Repeat ()
				.Select (client => new TcpChannel (client, new PacketBuffer (), tracerManager, configuration));
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
