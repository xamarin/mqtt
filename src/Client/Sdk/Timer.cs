using System.Threading.Tasks;

namespace System.Net.Mqtt.Sdk
{
	internal class Timer
	{
		volatile int intervalMillisecs;
		volatile bool isRunning;
		Task timerTask;

		public Timer () : this (intervalMillisecs: 0)
		{
		}

		public Timer (int intervalMillisecs, bool autoReset = true)
		{
			IntervalMillisecs = intervalMillisecs;
			AutoReset = autoReset;
		}

		public event EventHandler Elapsed = (sender, e) => {};

		public int IntervalMillisecs
		{
			get
			{
				return intervalMillisecs;
			}
			set
			{
				intervalMillisecs = value;
			}
		}

		public bool AutoReset { get; set; }

		public void Start ()
		{
			if (isRunning) {
				return;
			}

			if (IntervalMillisecs <= 0) {
				throw new InvalidOperationException ();
			}

			isRunning = true;
			timerTask = RunAsync ();
		}

		public void Reset ()
		{
			Stop ();
			Start ();
		}

		public void Stop ()
		{
			isRunning = false;
			timerTask = Task.FromResult (default (object));
		}

		async Task RunAsync ()
		{
			while (isRunning) {
				await Task.Delay (intervalMillisecs);

				if (isRunning) {
					Elapsed (this, EventArgs.Empty);

					if (!AutoReset) {
						Stop ();
					}
				}
			}
		}
	}
}
