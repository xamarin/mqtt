using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
	internal sealed class SingleThreadScheduler : TaskScheduler, IDisposable
	{
		BlockingCollection<Task> tasks;
		readonly Task runner;

		public SingleThreadScheduler ()
		{
			tasks = new BlockingCollection<Task> ();
			runner = new Task (() => {
				if (tasks == null) {
					return;
				}

				foreach (var task in tasks.GetConsumingEnumerable ()) {
					TryExecuteTask (task);
				}
			}, TaskCreationOptions.LongRunning);

			runner.Start (TaskScheduler.Default);
		}

		public override int MaximumConcurrencyLevel { get { return 1; } }

		protected override void QueueTask (Task task)
		{
			tasks.Add (task);
		}

		protected override IEnumerable<Task> GetScheduledTasks ()
		{
			return tasks.ToArray ();
		}

		protected override bool TryExecuteTaskInline (Task task, bool taskWasPreviouslyQueued)
		{
			return TryExecuteTask (task);
		}

		public void Dispose ()
		{
			Dispose (disposing: true);
			GC.SuppressFinalize (this);
		}

		void Dispose (bool disposing)
		{
			if (disposing) {
				if (tasks != null) {
					tasks.CompleteAdding ();
					tasks = null;
				}
			}
		}
	}
}