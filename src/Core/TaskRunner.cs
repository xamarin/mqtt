using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
	public class TaskRunner
	{
		private readonly TaskFactory taskFactory;

		private TaskRunner ()
		{
			this.taskFactory = new TaskFactory(CancellationToken.None, 
				TaskCreationOptions.DenyChildAttach, 
				TaskContinuationOptions.None, 
				new SingleThreadScheduler());
		}

		public static TaskRunner Get()
		{
			return new TaskRunner ();
		}

		public Task Run(Func<Task> func)
		{
			return taskFactory.StartNew(func).Unwrap();
		}

		public Task<T> Run<T>(Func<Task<T>> func)
		{
			return taskFactory.StartNew(func).Unwrap();
		}

		public Task Run(Action action)
		{
			return taskFactory.StartNew(action);
		}

		public Task<T> Run<T>(Func<T> func)
		{
			return taskFactory.StartNew(func);
		}
	}
}
