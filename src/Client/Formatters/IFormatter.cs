using System.Threading.Tasks;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Formatters
{
	internal interface IFormatter
	{
		/// <summary>
		/// Gets the type of packet that this formatter support.
		/// </summary>
		MqttPacketType PacketType { get; }

		/// <exception cref="MqttConnectionException">ConnectProtocolException</exception>
		/// <exception cref="MqttViolationException">ProtocolViolationException</exception>
		/// <exception cref="MqttException">ProtocolException</exception>
		Task<IPacket> FormatAsync (byte[] bytes);

		/// <exception cref="MqttConnectionException">ConnectProtocolException</exception>
		/// <exception cref="MqttViolationException">ProtocolViolationException</exception>
		/// <exception cref="MqttException">ProtocolException</exception>
		Task<byte[]> FormatAsync (IPacket packet);
	}
}
