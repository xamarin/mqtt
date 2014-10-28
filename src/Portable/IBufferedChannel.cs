using System;
using System.Threading.Tasks;

namespace Hermes
{
	public interface IBufferedChannel<T>
	{
		IObservable<T> Receiver { get; }

        Task SendAsync(T[] message);

		void Close();
	}
}
