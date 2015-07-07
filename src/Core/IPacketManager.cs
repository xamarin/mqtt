using System.Threading.Tasks;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt
{
	internal interface IPacketManager
	{
		/// <exception cref="MqttConnectionException">ConnectProtocolException</exception>
		/// <exception cref="MqttViolationException">ProtocolViolationException</exception>
		/// <exception cref="MqttException">ProtocolException</exception>
		Task<IPacket> GetPacketAsync (byte[] bytes);

		/// <exception cref="MqttConnectionException">ConnectProtocolException</exception>
		/// <exception cref="MqttViolationException">ProtocolViolationException</exception>
		/// <exception cref="MqttException">ProtocolException</exception>
		Task<byte[]> GetBytesAsync (IPacket packet);
	}
}
