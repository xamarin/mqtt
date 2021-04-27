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

		volatile bool closed;

		readonly WebSocket client;
		readonly IPacketBuffer buffer;
		readonly MqttConfiguration configuration;
		readonly ReplaySubject<byte[]> receiver;
		readonly ReplaySubject<byte[]> sender;
		readonly IDisposable streamSubscription;
		readonly AsyncLock asyncLockObject;

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
			asyncLockObject = new AsyncLock();
		}

		public bool IsConnected
		{
			get
			{
				var connected = !closed;

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
			if (!closed)
			{
				using (await asyncLockObject.LockAsync().ConfigureAwait(continueOnCapturedContext: false))
				{
					if (!closed)
					{
						if (!IsConnected)
						{
							throw new MqttException(Properties.Resources.MqttChannel_ClientNotConnected);
						}

						sender.OnNext(message);

						try
						{
							tracer.Verbose(Properties.Resources.MqttChannel_SendingPacket, message.Length);

							var segment = new ArraySegment<byte>(message);

							await client
								.SendAsync(segment, WebSocketMessageType.Binary, endOfMessage: true, cancellationToken: CancellationToken.None)
								.ConfigureAwait(continueOnCapturedContext: false);
						}
						catch (ObjectDisposedException disposedEx)
						{
							throw new MqttException(Properties.Resources.MqttChannel_StreamDisconnected, disposedEx);
						}
					}
				}
			}
		}

		public async Task CloseAsync()
		{
			if (!closed)
			{
				using (await asyncLockObject.LockAsync().ConfigureAwait(continueOnCapturedContext: false))
				{
					if (!closed)
					{
						tracer.Info(Properties.Resources.Mqtt_Disposing, GetType().FullName);

						streamSubscription.Dispose();
						receiver.OnCompleted();
						sender.OnCompleted();

						try
						{
							client?.CloseAsync(WebSocketCloseStatus.NormalClosure, "Dispose", CancellationToken.None).Wait();
							client?.Dispose();
						}
						catch (SocketException socketEx)
						{
							tracer.Error(socketEx, Properties.Resources.MqttChannel_DisposeError, socketEx.SocketErrorCode);
						}

						closed = true;
					}
				}
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
			.Subscribe (bytes => {
				if (buffer.TryGetPackets (bytes, out var packets)) {
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
