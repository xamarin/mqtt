using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using ClientProperties = System.Net.Mqtt.Properties;

namespace System.Net.Mqtt.Sdk.Bindings
{
	internal class PrivateChannel : IMqttChannel<byte[]>
	{
		static readonly ITracer tracer = Tracer.Get<PrivateChannel>();

		volatile bool closed;

		readonly PrivateStream stream;
		readonly EndpointIdentifier identifier;
		readonly ReplaySubject<byte[]> receiver;
		readonly ReplaySubject<byte[]> sender;
		readonly IDisposable streamSubscription;
		readonly AsyncLock asyncLockObject;

		public PrivateChannel(PrivateStream stream, EndpointIdentifier identifier, MqttConfiguration configuration)
		{
			this.stream = stream;
			this.identifier = identifier;
			receiver = new ReplaySubject<byte[]>(window: TimeSpan.FromSeconds(configuration.WaitTimeoutSecs));
			sender = new ReplaySubject<byte[]>(window: TimeSpan.FromSeconds(configuration.WaitTimeoutSecs));
			streamSubscription = SubscribeStream();
			asyncLockObject = new AsyncLock();
		}

		public bool IsConnected => !stream.IsDisposed;

		public IObservable<byte[]> ReceiverStream => receiver;

		public IObservable<byte[]> SenderStream => sender;

		public async Task SendAsync(byte[] message)
		{
			if (!closed)
			{
				using (await asyncLockObject.LockAsync().ConfigureAwait(continueOnCapturedContext: false))
				{
					if (!closed)
					{
						if (!IsConnected)
						{
							throw new MqttException(ClientProperties.Resources.MqttChannel_ClientNotConnected);
						}

						sender.OnNext(message);

						try
						{
							tracer.Verbose(ClientProperties.Resources.MqttChannel_SendingPacket, message.Length);

							stream.Send(message, identifier);
						}
						catch (ObjectDisposedException disposedEx)
						{
							throw new MqttException(ClientProperties.Resources.MqttChannel_StreamDisconnected, disposedEx);
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
						tracer.Info(Properties.Resources.Mqtt_Disposing, nameof(PrivateChannel));

						streamSubscription.Dispose();
						receiver.OnCompleted();
						sender.OnCompleted();
						stream.Dispose();

						closed = true;
					}
				}
			}
		}

		IDisposable SubscribeStream()
		{
			var senderIdentifier = identifier == EndpointIdentifier.Client ?
				EndpointIdentifier.Server :
				EndpointIdentifier.Client;

			return stream
				.Receive(senderIdentifier)
				.Subscribe(packet =>
				{
					tracer.Verbose(ClientProperties.Resources.MqttChannel_ReceivedPacket, packet.Length);

					receiver.OnNext(packet);
				}, ex =>
				{
					if (ex is ObjectDisposedException)
					{
						receiver.OnError(new MqttException(ClientProperties.Resources.MqttChannel_StreamDisconnected, ex));
					}
					else
					{
						receiver.OnError(ex);
					}
				}, () =>
				{
					tracer.Warn(ClientProperties.Resources.MqttChannel_NetworkStreamCompleted);
					receiver.OnCompleted();
				});
		}
	}
}
