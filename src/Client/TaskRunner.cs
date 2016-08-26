using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
	internal class TaskRunner : IDisposable
	{
		TaskFactory taskFactory;
		bool disposed;

		private TaskRunner ()
		{
			taskFactory = new TaskFactory (CancellationToken.None,
				TaskCreationOptions.DenyChildAttach,
				TaskContinuationOptions.None,
				new SingleThreadScheduler ());
		}

		public static TaskRunner Get ()
		{
			return new TaskRunner ();
		}

		public Task Run (Func<Task> func)
		{
			if (disposed) {
				throw new ObjectDisposedException (GetType ().FullName);
			}

			return taskFactory.StartNew (func).Unwrap ();
		}

		public Task<T> Run<T> (Func<Task<T>> func)
		{
			if (disposed) {
				throw new ObjectDisposedException (GetType ().FullName);
			}

			return taskFactory.StartNew (func).Unwrap ();
		}

		public Task Run (Action action)
		{
			if (disposed) {
				throw new ObjectDisposedException (GetType ().FullName);
			}

			return taskFactory.StartNew (action);
		}

		public Task<T> Run<T> (Func<T> func)
		{
			if (disposed) {
				throw new ObjectDisposedException (GetType ().FullName);
			}

			return taskFactory.StartNew (func);
		}

		public void Dispose ()
		{
			Dispose (disposing: true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposed)
				return;

			if (disposing) {
				(taskFactory.Scheduler as IDisposable)?.Dispose ();
				taskFactory = null;
				disposed = true;
			}
		}
	}
}
