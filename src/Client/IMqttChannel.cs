using System.Threading.Tasks;

namespace System.Net.Mqtt
{
	public interface IMqttChannel<T> : IDisposable
	{
		bool IsConnected { get; }

		IObservable<T> ReceiverStream { get; }

		IObservable<T> SenderStream { get; }

		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task SendAsync (T message);
	}
}
