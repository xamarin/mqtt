using System.Collections.Generic;
using System.Net.Mqtt.Sdk;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
	public class PacketIdProviderSpec
	{
		[Fact]
		public void when_getting_packet_id_then_is_sequencial()
		{
			var packetIdProvider = new PacketIdProvider ();
			var count = 5000;

			for (ushort i = 1; i <= count; i++) {
				packetIdProvider.GetPacketId ();
			}

			var packetId = packetIdProvider.GetPacketId ();

			Assert.Equal (count + 1, packetId);
		}

		[Fact]
		public void when_getting_packet_id_and_reaches_limit_then_resets()
		{
			var packetIdProvider = new PacketIdProvider ();

			for (var i = 1; i <= ushort.MaxValue; i++) {
				packetIdProvider.GetPacketId ();
			}

			var packetId = packetIdProvider.GetPacketId ();

			Assert.Equal ((ushort)1, packetId);
		}

		[Fact]
		public void when_getting_packet_id_in_parallel_then_maintains_sequence()
		{
			var packetIdProvider = new PacketIdProvider ();
			var count = 5000;
			var packetIdTasks = new List<Task> ();

			for (ushort i = 1; i <= count; i++) {
				packetIdTasks.Add(Task.Run(() => packetIdProvider.GetPacketId ()));
			}

			Task.WaitAll (packetIdTasks.ToArray());

			var packetId = packetIdProvider.GetPacketId ();

			Assert.Equal (count + 1, packetId);
		}
	}
}
