using System.Diagnostics;

namespace System.Net.Mqtt.Diagnostics
{
	static partial class Tracer
	{
		static Tracer ()
		{
			Initialize (new TracerManager ());
		}

		public static ITracerManager Manager
		{
			get { return manager; }
		}

		public static ITracerManager SetManager (ITracerManager manager)
		{
			Initialize (manager);

			return manager;
		}

		partial class DefaultManager
		{
			public void SetTracingLevel (string sourceName, SourceLevels level)
			{
			}

			public void AddListener (string sourceName, TraceListener listener)
			{
			}

			public void RemoveListener (string sourceName, TraceListener listener)
			{
			}

			public void RemoveListener (string sourceName, string listenerName)
			{
			}
		}
	}

	partial interface ITracerManager
	{
		void SetTracingLevel (string sourceName, SourceLevels level);

		void AddListener (string sourceName, TraceListener listener);

		void RemoveListener (string sourceName, TraceListener listener);

		void RemoveListener (string sourceName, string listenerName);
	}
}
