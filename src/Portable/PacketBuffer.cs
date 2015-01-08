using System.Collections.Generic;
using System.Linq;

namespace Hermes
{
	public class PacketBuffer : IPacketBuffer
	{
		bool readStarted;
		bool remainingLengthRead;
		int remainingLength = 0;
		bool packetReady;

		readonly IList<byte> mainBuffer;
		readonly IList<byte> pendingBuffer;

		public PacketBuffer ()
		{
			this.mainBuffer = new List<byte> ();
			this.pendingBuffer = new List<byte> ();
		}

		public bool TryGetPacket (byte[] sequence, out byte[] packet)
		{
			this.Buffer (sequence);

			if (this.packetReady) {
				packet = this.mainBuffer.ToArray ();
				this.Reset ();
			} else {
				packet = default (byte[]);
			}

			return packet != default(byte[]);
		}

		private void Buffer(byte[] sequence)
		{
			foreach(var @byte in sequence) {
				this.Buffer (@byte);
			}
		}

		private void Buffer (byte @byte)
		{
			if (this.packetReady) {
				this.pendingBuffer.Add (@byte);
				return;
			}

			this.mainBuffer.Add(@byte);

			if (!this.readStarted)
			{
				this.readStarted = true;
				return;
			}

			if (!this.remainingLengthRead)
			{
				if ((@byte & 128) == 0) {
					var bytesLenght = default (int);

					this.remainingLength = Protocol.Encoding.DecodeRemainingLength(mainBuffer.ToArray(), out bytesLenght);
					this.remainingLengthRead = true;

					if (remainingLength == 0)
						this.packetReady = true;
				}

				return;
			}

			if (remainingLength == 1)
			{
				this.packetReady = true;
			}
			else
			{
				remainingLength--;
			}
		}

		private void Reset()
		{
			this.mainBuffer.Clear();
			this.readStarted = false;
			this.remainingLengthRead = false;
			this.remainingLength = 0;
			this.packetReady = false;

			if (this.pendingBuffer.Any ()) {
				var pendingSequence = this.pendingBuffer.ToArray ();

				this.pendingBuffer.Clear ();
				this.Buffer (pendingSequence);
			}
		}
	}
}
