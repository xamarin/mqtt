using System;
using System.Threading.Tasks;

namespace Hermes
{
	public interface IChannel<T> : IDisposable
    {
		bool IsConnected { get; }

        IObservable<T> Receiver { get; }

		IObservable<T> Sender { get; }

        Task SendAsync(T message);
    }
}
