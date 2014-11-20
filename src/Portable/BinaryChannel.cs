using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Hermes
{
	public class BinaryChannel : IChannel<byte[]>
	{	
		bool readStarted;
		bool remainingLengthRead;
		int remainingLength;
		bool isPacketProcessed;

		readonly IList<byte> buffer;
		readonly IBufferedChannel<byte> innerChannel;
		readonly Subject<byte[]> receiver;
		readonly IDisposable subscription;

		public BinaryChannel (IBufferedChannel<byte> innerChannel)
		{
			this.buffer = new List<byte> ();
			this.remainingLength = 0;

			this.receiver = new Subject<byte[]> ();
			this.innerChannel = innerChannel;

			this.subscription = this.innerChannel.Receiver.Subscribe (@byte => {
				this.Process (@byte);

				if (this.isPacketProcessed) {
					this.receiver.OnNext(this.GetPacket ());
				}
			}, onError: ex => this.receiver.OnError(ex), onCompleted: () => this.receiver.OnCompleted());
		}

		public IObservable<byte[]> Receiver { get { return this.receiver; } }

		public async Task SendAsync (byte[] message)
		{
			await this.innerChannel.SendAsync (message);
		}

		public void Close ()
		{
			this.innerChannel.Close ();
			this.subscription.Dispose ();
			this.receiver.Dispose ();
		}

		private void Process (byte @byte)
		{
			if (this.isPacketProcessed) {
				return;
			}

			this.buffer.Add(@byte);

			if (!this.readStarted)
			{
				this.readStarted = true;
				return;
			}

			if (!this.remainingLengthRead)
			{
				if ((@byte & 128) == 0) {
					var bytesLenght = default (int);

					this.remainingLength = Protocol.Encoding.DecodeRemainingLength(buffer.ToArray(), out bytesLenght);
					this.remainingLengthRead = true;

					if (remainingLength == 0)
						this.isPacketProcessed = true;
				}

				return;
			}

			if (remainingLength == 1)
			{
				this.isPacketProcessed = true;
			}
			else
			{
				remainingLength--;
			}
		}

		private byte[] GetPacket()
		{
			if (!this.isPacketProcessed)
				return default (byte[]);

			var packet = this.buffer.ToArray ();

			this.ClearStreamingState();

			return packet;
		}

		private void ClearStreamingState()
		{
			this.buffer.Clear();
			this.readStarted = false;
			this.remainingLengthRead = false;
			this.remainingLength = 0;
			this.isPacketProcessed = false;
		}
	}
}
