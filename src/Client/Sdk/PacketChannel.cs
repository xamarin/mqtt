using System.Diagnostics;
using System.Net.Mqtt.Sdk.Packets;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Sdk
{
	internal class PacketChannel : IMqttChannel<IPacket>
	{
        static readonly ITracer tracer = Tracer.Get<PacketChannel> ();

        bool disposed;
		
		readonly IMqttChannel<byte[]> innerChannel;
		readonly IPacketManager manager;
		readonly ReplaySubject<IPacket> receiver;
		readonly ReplaySubject<IPacket> sender;
		readonly IDisposable subscription;

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
				.Subscribe (async bytes => {
					try {
						var packet = await this.manager.GetPacketAsync (bytes)
							.ConfigureAwait(continueOnCapturedContext: false);

						receiver.OnNext (packet);
					} catch (MqttException ex) {
						receiver.OnError (ex);
					}
				}, onError: ex => receiver.OnError (ex), onCompleted: () => receiver.OnCompleted ());
		}

		public bool IsConnected { get { return innerChannel != null && innerChannel.IsConnected; } }

		public IObservable<IPacket> ReceiverStream { get { return receiver; } }

		public IObservable<IPacket> SenderStream { get { return sender; } }

		public async Task SendAsync (IPacket packet)
		{
			if (disposed) {
				throw new ObjectDisposedException (GetType ().FullName);
			}

			var bytes = await manager.GetBytesAsync (packet)
				.ConfigureAwait(continueOnCapturedContext: false);

			sender.OnNext (packet);

			await innerChannel.SendAsync (bytes)
				.ConfigureAwait (continueOnCapturedContext: false);
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

				subscription.Dispose ();
				receiver.OnCompleted ();
				innerChannel.Dispose ();
				disposed = true;
			}
		}
	}
}
