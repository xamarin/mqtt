using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Sdk.Bindings
{
	internal class TcpChannel : IMqttChannel<byte[]>
	{
		static readonly ITracer tracer = Tracer.Get<TcpChannel>();

		volatile bool closed;

		readonly TcpClient client;
		readonly IPacketBuffer buffer;
		readonly ReplaySubject<byte[]> receiver;
		readonly ReplaySubject<byte[]> sender;
		readonly IDisposable streamSubscription;
		readonly AsyncLock asyncLockObject;

		public TcpChannel (TcpClient client, 
			IPacketBuffer buffer,
			MqttConfiguration configuration)
		{
			this.client = client;
			this.client.ReceiveBufferSize = configuration.BufferSize;
			this.client.SendBufferSize = configuration.BufferSize;
			this.buffer = buffer;
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
					connected = connected && client.Connected;
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

							await client
								.GetStream()
								.WriteAsync(message, 0, message.Length)
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
				var buffer = new byte[client.ReceiveBufferSize];

				return Observable.FromAsync<int> (() => {
					return client.GetStream ().ReadAsync (buffer, 0, buffer.Length);
				})
				.Select (x => buffer.Take (x));
			})
			.Repeat ()
			.TakeWhile (bytes => bytes.Any ())
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
