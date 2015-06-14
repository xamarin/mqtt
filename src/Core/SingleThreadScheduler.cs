using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
	internal sealed class SingleThreadScheduler : TaskScheduler, IDisposable
	{
		private BlockingCollection<Task> tasks;
		private readonly Thread thread;

		public SingleThreadScheduler (string name = null)
		{ 
			this.tasks = new BlockingCollection<Task> (); 
			this.thread = new Thread (() => { 
				foreach (var task in this.tasks.GetConsumingEnumerable()) { 
					TryExecuteTask (task); 
				} 
			}); 
			this.thread.IsBackground = true; 
			this.thread.SetApartmentState (ApartmentState.STA);

			if (this.thread.Name == null && !string.IsNullOrEmpty (name)) {
				this.thread.Name = name;
			}

			this.thread.Start ();
		}

		public override int MaximumConcurrencyLevel { get { return 1; } }

		protected override void QueueTask (Task task)
		{ 
			this.tasks.Add (task); 
		}

		protected override IEnumerable<Task> GetScheduledTasks ()
		{ 
			return this.tasks.ToArray (); 
		}

		protected override bool TryExecuteTaskInline (Task task, bool taskWasPreviouslyQueued)
		{ 
			return Thread.CurrentThread.GetApartmentState () == ApartmentState.STA && TryExecuteTask (task); 
		}

		public void Dispose ()
		{ 
			if (this.tasks != null) { 
				this.tasks.CompleteAdding (); 

				thread.Join (); 

				this.tasks.Dispose (); 
				this.tasks = null; 
			} 
		}
	}
}