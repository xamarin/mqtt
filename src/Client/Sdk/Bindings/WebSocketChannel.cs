using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Sdk.Bindings
{
	internal class WebSocketChannel : IMqttChannel<byte[]>
	{
		static readonly ITracer tracer = Tracer.Get<WebSocketChannel>();

		bool disposed;

		readonly WebSocket client;
		readonly IPacketBuffer buffer;
		readonly MqttConfiguration configuration;
		readonly ReplaySubject<byte[]> receiver;
		readonly ReplaySubject<byte[]> sender;
		readonly IDisposable streamSubscription;

		public WebSocketChannel (WebSocket client,
			IPacketBuffer buffer,
			MqttConfiguration configuration)
		{
			this.client = client;
			this.buffer = buffer;
			this.configuration = configuration;
			receiver = new ReplaySubject<byte[]> (window: TimeSpan.FromSeconds (configuration.WaitTimeoutSecs));
			sender = new ReplaySubject<byte[]> (window: TimeSpan.FromSeconds (configuration.WaitTimeoutSecs));
			streamSubscription = SubscribeStream ();
		}

		public bool IsConnected
		{
			get
			{
				var connected = !disposed;

				try {
					connected = connected && client.State == WebSocketState.Open;
				} catch (Exception) {
					connected = false;
				}

				return connected;
			}
		}

		public IObservable<byte[]> ReceiverStream { get { return receiver; } }

		public IObservable<byte[]> SenderStream { get { return sender; } }

		public async Task SendAsync (byte[] message)
		{
			if (disposed) {
				throw new ObjectDisposedException (GetType().FullName);
			}

			if (!IsConnected) {
				throw new MqttException (Properties.Resources.MqttChannel_ClientNotConnected);
			}

			sender.OnNext (message);

			try {
				tracer.Verbose (Properties.Resources.MqttChannel_SendingPacket, message.Length);

				var segment = new ArraySegment<byte> (message);

				await client
					.SendAsync (segment, WebSocketMessageType.Binary, endOfMessage: true, cancellationToken: CancellationToken.None)
					.ConfigureAwait (continueOnCapturedContext: false);
			} catch (ObjectDisposedException disposedEx) {
				throw new MqttException (Properties.Resources.MqttChannel_StreamDisconnected, disposedEx);
			}
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
				tracer.Info (Properties.Resources.Mqtt_Disposing, GetType ().FullName);

				streamSubscription.Dispose ();
				receiver.OnCompleted ();

				try {
					client?.CloseAsync (WebSocketCloseStatus.NormalClosure, "Dispose", CancellationToken.None).Wait ();
					client?.Dispose ();
				}
				catch (SocketException socketEx) {
					tracer.Error (socketEx, Properties.Resources.MqttChannel_DisposeError, socketEx.SocketErrorCode);
				}

				disposed = true;
			}
		}

		IDisposable SubscribeStream ()
		{
			return Observable.Defer (() => {
				var buffer = new byte[configuration.BufferSize];

				return Observable.FromAsync (() => {
					return client.ReceiveAsync (new ArraySegment<byte> (buffer), CancellationToken.None);
				})
				.Select (x => buffer.Take (x.Count));
			})
			.Repeat ()
			.TakeWhile (_ => IsConnected)
			.ObserveOn (NewThreadScheduler.Default)
			.Subscribe (bytes => {
				var packets = default (IEnumerable<byte[]>);

				if (buffer.TryGetPackets (bytes, out packets)) {
					foreach (var packet in packets) {
						tracer.Verbose (Properties.Resources.MqttChannel_ReceivedPacket, packet.Length);

						receiver.OnNext (packet);
					}
				}
			}, ex => {
				if (ex is ObjectDisposedException) {
					receiver.OnError (new MqttException (Properties.Resources.MqttChannel_StreamDisconnected, ex));
				} else {
					receiver.OnError (ex);
				}
			}, () => {
				tracer.Warn (Properties.Resources.MqttChannel_NetworkStreamCompleted);
				receiver.OnCompleted ();
			});
		}
	}
}
