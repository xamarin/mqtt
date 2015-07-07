using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Exceptions;

namespace System.Net.Mqtt.Server
{
	internal class TcpChannelProvider : IChannelProvider
	{
		static readonly ITracer tracer = Tracer.Get<TcpChannelProvider> ();

		readonly ProtocolConfiguration configuration;
		readonly Lazy<TcpListener> listener;
		bool disposed;

		public TcpChannelProvider (ProtocolConfiguration configuration)
		{
			this.configuration = configuration;
			this.listener = new Lazy<TcpListener> (() => {
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
			if (this.disposed) {
				throw new ObjectDisposedException (this.GetType ().FullName);
			}

			return Observable
				.FromAsync (() => {
					return Task.Factory.FromAsync<TcpClient> (this.listener.Value.BeginAcceptTcpClient,
						this.listener.Value.EndAcceptTcpClient, TaskCreationOptions.AttachedToParent);
				})
				.Repeat ()
				.Select (client => new TcpChannel (client, new PacketBuffer (), this.configuration));
		}

		public void Dispose ()
		{
			this.Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (this.disposed) return;

			if (disposing) {
				this.listener.Value.Stop ();
				this.disposed = true;
			}
		}
	}
}
