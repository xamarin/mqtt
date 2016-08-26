using System.Threading.Tasks;

namespace System.Net.Mqtt
{
	public interface IMqttChannel<T> : IDisposable
	{
		bool IsConnected { get; }

		IObservable<T> Receiver { get; }

		IObservable<T> Sender { get; }

		/// <exception cref="ProtocolException">ProtocolException</exception>
		Task SendAsync (T message);
	}
}
