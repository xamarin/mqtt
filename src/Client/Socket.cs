using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using ReactiveSockets;

namespace Hermes
{
	public class Socket : IBufferedChannel<byte>
	{
		readonly Subject<byte> receiver;
		readonly IReactiveSocket reactiveSocket;
		readonly IDisposable subscription;

		public Socket (IReactiveSocket reactiveSocket)
		{
			this.reactiveSocket = reactiveSocket;

			this.receiver = new Subject<byte> ();

			this.subscription = this.reactiveSocket.Receiver.Subscribe(@byte => {
				this.receiver.OnNext (@byte);
			}, ex => this.receiver.OnError(ex), () => this.receiver.OnCompleted());
		}

		public IObservable<byte> Receiver { get { return this.receiver; } }

		public async Task SendAsync (byte[] message)
		{
			await this.reactiveSocket.SendAsync (message);
		}

		public void Close ()
		{
			this.reactiveSocket.Dispose ();
			this.subscription.Dispose ();
			this.receiver.Dispose ();
		}
	}
}
