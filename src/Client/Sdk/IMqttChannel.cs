using System.Threading.Tasks;

namespace System.Net.Mqtt.Sdk
{
	/// <summary>
	/// Represents a mechanism to send and receive information between two endpoints
	/// </summary>
	/// <typeparam name="T">The type of information that will go through the channel</typeparam>
	public interface IMqttChannel<T>
	{
		/// <summary>
		/// Indicates if the channel is connected to the underlying stream or not
		/// </summary>
		bool IsConnected { get; }

		/// <summary>
		/// Represents the stream of incoming information received by the channel
		/// </summary>
		IObservable<T> ReceiverStream { get; }

		/// <summary>
		/// Represents the stream of outgoing information sent by the channel
		/// </summary>
		IObservable<T> SenderStream { get; }

		/// <summary>
		/// Sends information to the other end, through the underlying stream
		/// </summary>
		/// <param name="message">
		/// Message to send to the other end of the channel
		/// </param>
		/// <exception cref="MqttException">MqttException</exception>
		Task SendAsync(T message);

		/// <summary>
		/// Closes the channel for communication and completes all the associated streams
		/// </summary>
		/// <returns></returns>
		Task CloseAsync();
	}
}
