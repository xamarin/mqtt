using System;
using System.Net.Sockets;
using System.Reactive.Disposables;
using Hermes.Diagnostics;
using Hermes.Properties;

namespace Hermes
{
	public class ChannelObservable : IObservable<IChannel<byte[]>>
	{
		private static readonly ITracer tracer = Tracer.Get<ChannelObservable> ();

		private readonly TcpListener listener;
		private readonly IObservable<IChannel<byte[]>> innerObservable;

		private bool isStarted;

		public ChannelObservable (TcpListener listener, IObservable<IChannel<byte[]>> innerObservable)
		{
			this.listener = listener;
			this.innerObservable = innerObservable;
		}

		public IDisposable Subscribe (IObserver<IChannel<byte[]>> observer)
		{
			if (!this.isStarted) {
				try {
					this.listener.Start ();
					this.isStarted = true;
				} catch (SocketException socketEx) {
					tracer.Error (socketEx);

					throw new ProtocolException (Resources.ChannelObservable_TcpListener_Failed, socketEx);
				}
			}

			return new CompositeDisposable (innerObservable.Subscribe (observer),
				Disposable.Create (() => this.listener.Stop ()));
		}
	}
}
