﻿using System.Threading.Tasks;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Exceptions;

namespace System.Net.Mqtt.Formatters
{
	internal abstract class Formatter<T> : IFormatter
		where T : class, IPacket
	{
		public abstract PacketType PacketType { get; }

		protected abstract T Read (byte[] bytes);

		protected abstract byte[] Write (T packet);

		/// <exception cref="MqttConnectionException">ConnectProtocolException</exception>
		/// <exception cref="MqttViolationException">ProtocolViolationException</exception>
		/// <exception cref="MqttException">ProtocolException</exception>
		public async Task<IPacket> FormatAsync (byte[] bytes)
		{
			var actualType = (PacketType)bytes.Byte (0).Bits (4);

			if (PacketType != actualType) {
				var error = string.Format(Properties.Resources.Formatter_InvalidPacket, typeof(T).Name);

				throw new MqttException (error);
			}

			var packet = await Task.Run(() => Read (bytes))
				.ConfigureAwait(continueOnCapturedContext: false);

			return packet;
		}

		/// <exception cref="MqttConnectionException">ConnectProtocolException</exception>
		/// <exception cref="MqttViolationException">ProtocolViolationException</exception>
		/// <exception cref="MqttException">ProtocolException</exception>
		public async Task<byte[]> FormatAsync (IPacket packet)
		{
			if (packet.Type != PacketType) {
				var error = string.Format(Properties.Resources.Formatter_InvalidPacket, typeof(T).Name);

				throw new MqttException (error);
			}

			var bytes = await Task.Run(() => Write (packet as T))
				.ConfigureAwait(continueOnCapturedContext: false);

			return bytes;
		}

		protected void ValidateHeaderFlag (byte[] bytes, Func<PacketType, bool> packetTypePredicate, int expectedFlag)
		{
			var headerFlag = bytes.Byte (0).Bits (5, 4);

			if (packetTypePredicate (PacketType) && headerFlag != expectedFlag) {
				var error = string.Format (Properties.Resources.Formatter_InvalidHeaderFlag, headerFlag, typeof(T).Name, expectedFlag);

				throw new MqttException (error);
			}
		}
	}
}
