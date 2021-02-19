using System.Diagnostics;
using System.Net.Mqtt.Sdk.Packets;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Sdk
{
	internal class PacketChannel : IMqttChannel<IPacket>
	{
        static readonly ITracer tracer = Tracer.Get<PacketChannel> ();

		volatile bool closed;

		readonly IMqttChannel<byte[]> innerChannel;
		readonly IPacketManager manager;
		readonly ReplaySubject<IPacket> receiver;
		readonly ReplaySubject<IPacket> sender;
		readonly IDisposable subscription;
		readonly AsyncLock asyncLockObject;

		public PacketChannel (IMqttChannel<byte[]> innerChannel, 
			IPacketManager manager, 
			MqttConfiguration configuration)
		{
			this.innerChannel = innerChannel;
			this.manager = manager;

			receiver = new ReplaySubject<IPacket> (window: TimeSpan.FromSeconds (configuration.WaitTimeoutSecs));
			sender = new ReplaySubject<IPacket> (window: TimeSpan.FromSeconds (configuration.WaitTimeoutSecs));
			subscription = innerChannel
				.ReceiverStream
				.Subscribe(async bytes =>
				{
					try
					{
						var packet = await this.manager.GetPacketAsync(bytes)
							.ConfigureAwait(continueOnCapturedContext: false);

						receiver.OnNext(packet);
					}
					catch (MqttException ex)
					{
						receiver.OnError(ex);
					}
				}, onError: ex => receiver.OnError(ex), onCompleted: () => receiver.OnCompleted());
			asyncLockObject = new AsyncLock();
		}

		public bool IsConnected { get { return innerChannel != null && innerChannel.IsConnected; } }

		public IObservable<IPacket> ReceiverStream { get { return receiver; } }

		public IObservable<IPacket> SenderStream { get { return sender; } }

		public async Task SendAsync (IPacket packet)
		{
			if (!closed)
			{
				using (await asyncLockObject.LockAsync().ConfigureAwait(continueOnCapturedContext: false))
				{
					if (!closed)
					{
						var bytes = await manager
							.GetBytesAsync(packet)
							.ConfigureAwait(continueOnCapturedContext: false);

						sender.OnNext(packet);

						await innerChannel
							.SendAsync(bytes)
							.ConfigureAwait(continueOnCapturedContext: false);
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

						subscription.Dispose();
						receiver.OnCompleted();
						sender.OnCompleted();
						await innerChannel.CloseAsync().ConfigureAwait(continueOnCapturedContext: false);

						closed = true;
					}
				}
			}
		}
	}
}
