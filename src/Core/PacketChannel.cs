﻿using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Exceptions;

namespace System.Net.Mqtt
{
	internal class PacketChannel : IChannel<IPacket>
	{
		bool disposed;
		readonly ITracer tracer;
		readonly IChannel<byte[]> innerChannel;
		readonly IPacketManager manager;
		readonly ReplaySubject<IPacket> receiver;
		readonly ReplaySubject<IPacket> sender;
		readonly IDisposable subscription;

		public PacketChannel (IChannel<byte[]> innerChannel, 
			IPacketManager manager, 
			ITracerManager tracerManager,
			ProtocolConfiguration configuration)
		{
			tracer = tracerManager.Get<PacketChannel> ();
			this.innerChannel = innerChannel;
			this.manager = manager;

			receiver = new ReplaySubject<IPacket> (window: TimeSpan.FromSeconds (configuration.WaitingTimeoutSecs));
			sender = new ReplaySubject<IPacket> (window: TimeSpan.FromSeconds (configuration.WaitingTimeoutSecs));
			subscription = innerChannel.Receiver
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

		public IObservable<IPacket> Receiver { get { return receiver; } }

		public IObservable<IPacket> Sender { get { return sender; } }

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
				tracer.Info (Properties.Resources.Tracer_Disposing, GetType ().FullName);

				subscription.Dispose ();
				receiver.OnCompleted ();
				innerChannel.Dispose ();
				disposed = true;
			}
		}
	}
}
