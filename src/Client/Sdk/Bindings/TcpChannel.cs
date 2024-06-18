using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Sdk.Bindings
{
	internal static class Log {
		public static void WriteLine (ITracer tracer, string message)
		{
			var msg = $"{System.Diagnostics.Process.GetCurrentProcess ().Id} {System.DateTime.UtcNow.ToString ("O")} {message}";
			Console.Error.WriteLine (msg);
			tracer.Verbose(msg);
		}

		public static void WriteLine (ITracer tracer, string format, params string[] args)
		{
			WriteLine (tracer, string.Format (format, args));
		}

		public static string AsString (this byte[] bytes)
		{
			var sb = new System.Text.StringBuilder ();
			sb.Append ($"Length: {bytes.Length}");
			for (var i = 0; i < Math.Min (16, bytes.Length); i++) {
				var b = bytes [i];
				if (b > 20) {
					sb.Append ($" 0x{b:2x} = {(char) b}");
				} else {
					sb.Append ($" 0x{b:2x} = ?");
				}
			}
			return sb.ToString ();
		}
	}

	internal class TcpChannel : IMqttChannel<byte[]>
	{
		static readonly ITracer tracer = Tracer.Get<TcpChannel>();

		volatile bool closed;
		volatile bool completed;

		readonly TcpClient client;
		readonly IPacketBuffer buffer;
		readonly ReplaySubject<byte[]> receiver;
		readonly ReplaySubject<byte[]> sender;
		readonly IDisposable streamSubscription;
		readonly AsyncLock asyncLockObject;

		public TcpChannel(TcpClient client,
			IPacketBuffer buffer,
			MqttConfiguration configuration)
		{
			Log.WriteLine (tracer, $"System.Net.Mqtt.Sdk.Bindings.TcpChannel () {System.Environment.StackTrace}");
			this.client = client;
			this.client.ReceiveBufferSize = configuration.BufferSize;
			this.client.SendBufferSize = configuration.BufferSize;
			this.buffer = buffer;
			receiver = new ReplaySubject<byte[]>(window: TimeSpan.FromSeconds(configuration.WaitTimeoutSecs));
			sender = new ReplaySubject<byte[]>(window: TimeSpan.FromSeconds(configuration.WaitTimeoutSecs));
			streamSubscription = SubscribeStream();
			asyncLockObject = new AsyncLock();
		}

		public bool IsConnected
		{
			get
			{
				try
				{
					return !closed && !completed && client.Connected;
				}
				catch (Exception)
				{
					return false;
				}
			}
		}

		public IObservable<byte[]> ReceiverStream { get { return receiver; } }

		public IObservable<byte[]> SenderStream { get { return sender; } }

		public async Task SendAsync(byte[] message)
		{
			if (!closed && !completed)
			{
				using (await asyncLockObject.LockAsync().ConfigureAwait(continueOnCapturedContext: false))
				{
					if (!closed && !completed)
					{
						if (!IsConnected)
						{
							throw new MqttException(Properties.Resources.MqttChannel_ClientNotConnected);
						}

						sender.OnNext(message);

						try
						{
							tracer.Verbose(Properties.Resources.MqttChannel_SendingPacket, message.Length);
							Log.WriteLine (tracer, $"System.Net.Mqtt.Sdk.Bindings.TcpChannel.SendAsync () bytes: {message.AsString ()}");

							await client
								.GetStream()
								.WriteAsync(message, 0, message.Length)
								.ConfigureAwait(continueOnCapturedContext: false);
							Log.WriteLine (tracer, $"System.Net.Mqtt.Sdk.Bindings.TcpChannel.SendAsync () DONE bytes: {message.AsString ()}");
						}
						catch (ObjectDisposedException disposedEx)
						{
							Log.WriteLine (tracer, $"System.Net.Mqtt.Sdk.Bindings.SendAsync () EXCEPTION bytes: {message.AsString ()} {disposedEx}");
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

		IDisposable SubscribeStream()
		{
			return Observable.Defer(() =>
			{
				var buffer = new byte[client.ReceiveBufferSize];

				return Observable.FromAsync(() =>
				{
					return client.GetStream().ReadAsync(buffer, 0, buffer.Length);
				})
				.Select(x => buffer.Take(x));
			})
			.Repeat()
			.TakeWhile(bytes => bytes.Any())
			.Subscribe(bytes =>
			{
				if (buffer.TryGetPackets(bytes, out var packets))
				{
					foreach (var packet in packets)
					{
						tracer.Verbose(Properties.Resources.MqttChannel_ReceivedPacket, packet.Length);
						receiver.OnNext(packet);
					}
				}
			}, ex =>
			{
				if (ex is ObjectDisposedException)
				{
					receiver.OnError(new MqttException(Properties.Resources.MqttChannel_StreamDisconnected, ex));
				}
				else
				{
					receiver.OnError(ex);
				}
			}, () =>
			{
				tracer.Warn(Properties.Resources.MqttChannel_NetworkStreamCompleted);
				completed = true;
				receiver.OnCompleted();
			});
		}
	}
}
