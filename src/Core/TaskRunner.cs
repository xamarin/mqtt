using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
	internal class TaskRunner
	{
		private readonly TaskFactory taskFactory;

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
