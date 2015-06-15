namespace System.Net.Mqtt
{
	internal class PacketIdProvider : IPacketIdProvider
	{
		private readonly object lockObject;
		private volatile ushort lastValue;

		public PacketIdProvider ()
		{
			this.lockObject = new object ();
			this.lastValue = 0;
		}

		public ushort GetPacketId ()
		{
			var id = default (ushort);

			lock (this.lockObject) {
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
