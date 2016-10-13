using System.Collections.Generic;
using System.Linq;

namespace System.Net.Mqtt.Sdk
{
    internal class PacketBuffer : IPacketBuffer
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
			mainBuffer = new List<byte> ();
			pendingBuffer = new List<byte> ();
		}

		public bool TryGetPackets (IEnumerable<byte> sequence, out IEnumerable<byte[]> packets)
		{
			var result =  new List<byte[]>();

			lock (bufferLock) {
				Buffer (sequence);

				while (isPacketReady) {
					var packet = mainBuffer.ToArray ();

					result.Add (packet);
					Reset ();
				}

				packets = result;
			}

			return result.Any ();
		}

		void Buffer (IEnumerable<byte> sequence)
		{
			foreach (var @byte in sequence) {
				Buffer (@byte);
			}
		}

		void Buffer (byte @byte)
		{
			if (isPacketReady) {
				pendingBuffer.Add (@byte);
				return;
			}

			mainBuffer.Add (@byte);

			if (!packetReadStarted) {
				packetReadStarted = true;
				return;
			}

			if (!packetRemainingLengthReadCompleted) {
				if ((@byte & 128) == 0) {
					var bytesLenght = default (int);

					packetRemainingLength = MqttProtocol.Encoding.DecodeRemainingLength (mainBuffer.ToArray (), out bytesLenght);
					packetRemainingLengthReadCompleted = true;

					if (packetRemainingLength == 0)
						isPacketReady = true;
				}

				return;
			}

			if (packetRemainingLength == 1) {
				isPacketReady = true;
			} else {
				packetRemainingLength--;
			}
		}

		void Reset ()
		{
			mainBuffer.Clear ();
			packetReadStarted = false;
			packetRemainingLengthReadCompleted = false;
			packetRemainingLength = 0;
			isPacketReady = false;

			if (pendingBuffer.Any ()) {
				var pendingSequence = pendingBuffer.ToArray ();

				pendingBuffer.Clear ();
				Buffer (pendingSequence);
			}
		}
	}
}
