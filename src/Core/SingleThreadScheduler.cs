using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
	internal sealed class SingleThreadScheduler : TaskScheduler, IDisposable
	{
		readonly Thread thread;
		BlockingCollection<Task> tasks;

		public SingleThreadScheduler (string name = null)
		{
			tasks = new BlockingCollection<Task> ();
			thread = new Thread (() => {
				foreach (var task in tasks.GetConsumingEnumerable ()) {
					TryExecuteTask (task);
				}
			});
			thread.IsBackground = true;
			thread.SetApartmentState (ApartmentState.STA);

			if (thread.Name == null && !string.IsNullOrEmpty (name)) {
				thread.Name = name;
			}

			thread.Start ();
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
			return Thread.CurrentThread.GetApartmentState () == ApartmentState.STA && TryExecuteTask (task);
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
					thread.Join ();
					tasks = null;
				}
			}
		}
	}
}