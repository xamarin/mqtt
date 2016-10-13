namespace System.Net.Mqtt.Sdk
{
	internal class PacketIdProvider : IPacketIdProvider
	{
		readonly object lockObject;
		volatile ushort lastValue;

		public PacketIdProvider ()
		{
			lockObject = new object ();
			lastValue = 0;
		}

		public ushort GetPacketId ()
		{
			var id = default (ushort);

			lock (lockObject) {
				if (lastValue == ushort.MaxValue) {
					id = 1;
				} else {
					id = (ushort)(lastValue + 1);
				}

				lastValue = id;
			}

			return id;
		}
	}
}
