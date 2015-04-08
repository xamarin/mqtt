using System.Collections.Generic;
using System.Linq;

namespace Hermes
{
	public class PacketBuffer : IPacketBuffer
	{
		bool packetReadStarted;
		bool packetRemainingLengthReadCompleted;
		int packetRemainingLength = 0;
		bool isPacketReady;

		readonly object bufferLock = new object ();
		readonly IList<byte> mainBuffer;
		readonly IList<byte> pendingBuffer;

		public PacketBuffer ()
		{
			this.mainBuffer = new List<byte> ();
			this.pendingBuffer = new List<byte> ();
		}

		public bool TryGetPackets (byte[] sequence, out IEnumerable<byte[]> packets)
		{
			var result =  new List<byte[]>();

			lock (bufferLock) {
				this.Buffer (sequence);

				while (this.isPacketReady) {
					var packet = this.mainBuffer.ToArray ();

					result.Add (packet);
					this.Reset ();
				}

				packets = result;
			}

			return result.Any();
		}

		private void Buffer(byte[] sequence)
		{
			foreach(var @byte in sequence) {
				this.Buffer (@byte);
			}
		}

		private void Buffer (byte @byte)
		{
			if (this.isPacketReady) {
				this.pendingBuffer.Add (@byte);
				return;
			}

			this.mainBuffer.Add(@byte);

			if (!this.packetReadStarted)
			{
				this.packetReadStarted = true;
				return;
			}

			if (!this.packetRemainingLengthReadCompleted)
			{
				if ((@byte & 128) == 0) {
					var bytesLenght = default (int);

					this.packetRemainingLength = Protocol.Encoding.DecodeRemainingLength(mainBuffer.ToArray(), out bytesLenght);
					this.packetRemainingLengthReadCompleted = true;

					if (packetRemainingLength == 0)
						this.isPacketReady = true;
				}

				return;
			}

			if (packetRemainingLength == 1)
			{
				this.isPacketReady = true;
			}
			else
			{
				packetRemainingLength--;
			}
		}

		private void Reset()
		{
			this.mainBuffer.Clear();
			this.packetReadStarted = false;
			this.packetRemainingLengthReadCompleted = false;
			this.packetRemainingLength = 0;
			this.isPacketReady = false;

			if (this.pendingBuffer.Any ()) {
				var pendingSequence = this.pendingBuffer.ToArray ();

				this.pendingBuffer.Clear ();
				this.Buffer (pendingSequence);
			}
		}
	}
}
