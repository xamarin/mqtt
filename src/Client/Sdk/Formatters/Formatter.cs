using System.Threading.Tasks;
using System.Net.Mqtt.Sdk.Packets;

namespace System.Net.Mqtt.Sdk.Formatters
{
	internal abstract class Formatter<T> : IFormatter
		where T : class, IPacket
	{
		public abstract MqttPacketType PacketType { get; }

		protected abstract T Read (byte[] bytes);

		protected abstract byte[] Write (T packet);

		public async Task<IPacket> FormatAsync (byte[] bytes)
		{
			var actualType = (MqttPacketType)bytes.Byte (0).Bits (4);

			if (PacketType != actualType) {
				var error = string.Format (Properties.Resources.Formatter_InvalidPacket, typeof(T).Name);

				throw new MqttException (error);
			}

			var packet = await Task.Run(() => Read (bytes))
				.ConfigureAwait(continueOnCapturedContext: false);

			return packet;
		}

		public async Task<byte[]> FormatAsync (IPacket packet)
		{
			if (packet.Type != PacketType) {
				var error = string.Format (Properties.Resources.Formatter_InvalidPacket, typeof(T).Name);

				throw new MqttException (error);
			}

			var bytes = await Task.Run(() => Write (packet as T))
				.ConfigureAwait(continueOnCapturedContext: false);

			return bytes;
		}

		protected void ValidateHeaderFlag (byte[] bytes, Func<MqttPacketType, bool> packetTypePredicate, int expectedFlag)
		{
			var headerFlag = bytes.Byte (0).Bits (5, 4);

			if (packetTypePredicate (PacketType) && headerFlag != expectedFlag) {
				var error = string.Format  (Properties.Resources.Formatter_InvalidHeaderFlag, headerFlag, typeof(T).Name, expectedFlag);

				throw new MqttException (error);
			}
		}
	}
}
