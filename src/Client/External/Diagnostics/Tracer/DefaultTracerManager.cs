namespace System.Net.Mqtt.Diagnostics
{
	using System;
	using System.Diagnostics;

	internal partial class DefaultTracerManager : ITracerManager
	{
		public ITracer Get (string name)
		{
			return new DefaultTracer (name);
		}

		private class DefaultTracer : ITracer
		{
			private string name;

			public DefaultTracer (string name)
			{
				this.name = name;
			}

			public void Trace (TraceEventType type, object message)
			{
				Trace (type, message.ToString ());
			}

			public void Trace (TraceEventType type, string format, params object[] args)
			{
				Trace (type, Format (format, args));
			}

			public void Trace (TraceEventType type, Exception exception, object message)
			{
				Trace (type, message + Environment.NewLine + exception.ToString ());
			}

			public void Trace (TraceEventType type, Exception exception, string format, params object[] args)
			{
				Trace (type, Format (format, args) + Environment.NewLine + exception.ToString ());
			}

			private void Trace (TraceEventType type, string message)
			{
				Debug.WriteLine (string.Format (
					"[{0}::{1}] ",
					this.name, type, message));
				Debug.WriteLine (message);
			}

			private string Format (string format, object[] args)
			{
				if (args != null && args.Length != 0)
					return string.Format (format, args);

				return format;
			}
		}
	}
}
