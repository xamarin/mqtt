using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Sdk
{
	internal class AsyncLock
	{
		readonly SemaphoreSlim semaphore;
		readonly Releaser releaser;

		public AsyncLock()
		{
			semaphore = new SemaphoreSlim(1, 1);
			releaser = new Releaser(this);
		}

		public async Task<IDisposable> LockAsync()
		{
			await semaphore.WaitAsync();

			return releaser;
		}

		private class Releaser : IDisposable
		{
			readonly AsyncLock lockObject;

			internal Releaser(AsyncLock lockObject)
			{
				this.lockObject = lockObject;
			}

			public void Dispose()
			{
				if (lockObject != null)
				{
					lockObject.semaphore.Release();
				}
			}
		}
	}
}
