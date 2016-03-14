using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
	internal class TaskRunner : IDisposable
	{
		private TaskFactory taskFactory;
		private bool disposed;

		private TaskRunner (string name = null)
		{
			this.taskFactory = new TaskFactory(CancellationToken.None, 
				TaskCreationOptions.DenyChildAttach, 
				TaskContinuationOptions.None, 
				new SingleThreadScheduler(name));
		}

		public static TaskRunner Get(string name = null)
		{
			return new TaskRunner (name);
		}

		public Task Run(Func<Task> func)
		{
			if (this.disposed) {
				throw new ObjectDisposedException (this.GetType ().FullName);
			}

			return taskFactory.StartNew(func).Unwrap();
		}

		public Task<T> Run<T>(Func<Task<T>> func)
		{
			if (this.disposed) {
				throw new ObjectDisposedException (this.GetType ().FullName);
			}

			return taskFactory.StartNew(func).Unwrap();
		}

		public Task Run(Action action)
		{
			if (this.disposed) {
				throw new ObjectDisposedException (this.GetType ().FullName);
			}

			return taskFactory.StartNew(action);
		}

		public Task<T> Run<T>(Func<T> func)
		{
			if (this.disposed) {
				throw new ObjectDisposedException (this.GetType ().FullName);
			}

			return taskFactory.StartNew(func);
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (this.disposed)
				return;

			if (disposing) {
				(this.taskFactory.Scheduler as IDisposable)?.Dispose();
				this.taskFactory = null;
			}
		}
	}
}
