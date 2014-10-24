using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveSockets;

namespace Hermes
{
	public class BinaryChannel : IChannel<byte[]>
	{
		readonly IReactiveSocket socket;

		public BinaryChannel (IReactiveSocket socket)
		{
			this.socket = socket;

			this.Received = from length in socket.Receiver
							let body = socket.Receiver.Take(length)
							select body.ToEnumerable().ToArray();
		}

		public IObservable<byte[]> Received { get; private set; }

		public async Task SendAsync (byte[] message)
		{
			await this.socket.SendAsync (message);
		}

		public void Close ()
		{
			this.socket.Dispose ();
		}
	}
}
