using System;
using System.Net.Sockets;
using System.Reactive.Disposables;

namespace Hermes
{
	public class ChannelObservable : IObservable<IChannel<byte[]>>
	{
		private readonly TcpListener listener;
		private readonly IObservable<IChannel<byte[]>> innerObservable;

		public ChannelObservable (TcpListener listener, IObservable<IChannel<byte[]>> innerObservable)
		{
			this.listener = listener;
			this.innerObservable = innerObservable;
		}

		public IDisposable Subscribe (IObserver<IChannel<byte[]>> observer)
		{
			return new CompositeDisposable (innerObservable.Subscribe (observer),
				Disposable.Create (() => this.listener.Stop ()));
		}
	}
}
