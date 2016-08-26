namespace System.Net.Mqtt.Diagnostics
{
	using System;

	/// <summary>
	/// Provides usability overloads for tracing to a <see cref="ITracer"/>.
	/// </summary>
	///	<nuget id="Tracer.Interfaces" />
	static partial class ITracerExtensions
	{
		#region Critical overloads

		/// <summary>
		/// Traces an event of type <see cref="TraceEventType.Critical"/> with the given message;
		/// </summary>
		public static void Critical (this ITracer tracer, object message)
		{
			tracer.Trace (TraceEventType.Critical, message);
		}

		/// <summary>
		/// Traces an event of type <see cref="TraceEventType.Critical"/> with the given format string and arguments.
		/// </summary>
		public static void Critical (this ITracer tracer, string format, params object[] args)
		{
			tracer.Trace (TraceEventType.Critical, format, args);
		}

		/// <summary>
		/// Traces an event of type <see cref="TraceEventType.Critical"/> with the given exception and message.
		/// </summary>
		public static void Critical (this ITracer tracer, Exception exception, object message)
		{
			tracer.Trace (TraceEventType.Critical, exception, message);
		}

		/// <summary>
		/// Traces an event of type <see cref="TraceEventType.Critical"/> with the given exception, format string and arguments.
		/// </summary>
		public static void Critical (this ITracer tracer, Exception exception, string format, params object[] args)
		{
			tracer.Trace (TraceEventType.Critical, exception, format, args);
		}

		#endregion

		#region Error overloads

		/// <summary>
		/// Traces an event of type <see cref="TraceEventType.Error"/> with the given message;
		/// </summary>
		public static void Error (this ITracer tracer, object message)
		{
			tracer.Trace (TraceEventType.Error, message);
		}

		/// <summary>
		/// Traces an event of type <see cref="TraceEventType.Error"/> with the given format string and arguments.
		/// </summary>
		public static void Error (this ITracer tracer, string format, params object[] args)
		{
			tracer.Trace (TraceEventType.Error, format, args);
		}

		/// <summary>
		/// Traces an event of type <see cref="TraceEventType.Error"/> with the given exception and message.
		/// </summary>
		public static void Error (this ITracer tracer, Exception exception, object message)
		{
			tracer.Trace (TraceEventType.Error, exception, message);
		}

		/// <summary>
		/// Traces an event of type <see cref="TraceEventType.Error"/> with the given exception, format string and arguments.
		/// </summary>
		public static void Error (this ITracer tracer, Exception exception, string format, params object[] args)
		{
			tracer.Trace (TraceEventType.Error, exception, format, args);
		}

		#endregion

		#region Warn overloads

		/// <summary>
		/// Traces an event of type <see cref="TraceEventType.Warning"/> with the given message;
		/// </summary>
		public static void Warn (this ITracer tracer, object message)
		{
			tracer.Trace (TraceEventType.Warning, message);
		}

		/// <summary>
		/// Traces an event of type <see cref="TraceEventType.Warning"/> with the given format string and arguments.
		/// </summary>
		public static void Warn (this ITracer tracer, string format, params object[] args)
		{
			tracer.Trace (TraceEventType.Warning, format, args);
		}

		/// <summary>
		/// Traces an event of type <see cref="TraceEventType.Warning"/> with the given exception and message.
		/// </summary>
		public static void Warn (this ITracer tracer, Exception exception, object message)
		{
			tracer.Trace (TraceEventType.Warning, exception, message);
		}

		/// <summary>
		/// Traces an event of type <see cref="TraceEventType.Warning"/> with the given exception, format string and arguments.
		/// </summary>
		public static void Warn (this ITracer tracer, Exception exception, string format, params object[] args)
		{
			tracer.Trace (TraceEventType.Warning, exception, format, args);
		}

		#endregion

		#region Info overloads

		/// <summary>
		/// Traces an event of type <see cref="TraceEventType.Information"/> with the given message;
		/// </summary>
		public static void Info (this ITracer tracer, object message)
		{
			tracer.Trace (TraceEventType.Information, message);
		}

		/// <summary>
		/// Traces an event of type <see cref="TraceEventType.Information"/> with the given format string and arguments.
		/// </summary>
		public static void Info (this ITracer tracer, string format, params object[] args)
		{
			tracer.Trace (TraceEventType.Information, format, args);
		}

		/// <summary>
		/// Traces an event of type <see cref="TraceEventType.Information"/> with the given exception and message.
		/// </summary>
		public static void Info (this ITracer tracer, Exception exception, object message)
		{
			tracer.Trace (TraceEventType.Information, exception, message);
		}

		/// <summary>
		/// Traces an event of type <see cref="TraceEventType.Information"/> with the given exception, format string and arguments.
		/// </summary>
		public static void Info (this ITracer tracer, Exception exception, string format, params object[] args)
		{
			tracer.Trace (TraceEventType.Information, exception, format, args);
		}

		#endregion

		#region Verbose overloads

		/// <summary>
		/// Traces an event of type <see cref="TraceEventType.Verbose"/> with the given message;
		/// </summary>
		public static void Verbose (this ITracer tracer, object message)
		{
			tracer.Trace (TraceEventType.Verbose, message);
		}

		/// <summary>
		/// Traces an event of type <see cref="TraceEventType.Verbose"/> with the given format string and arguments.
		/// </summary>
		public static void Verbose (this ITracer tracer, string format, params object[] args)
		{
			tracer.Trace (TraceEventType.Verbose, format, args);
		}

		/// <summary>
		/// Traces an event of type <see cref="TraceEventType.Verbose"/> with the given exception and message.
		/// </summary>
		public static void Verbose (this ITracer tracer, Exception exception, object message)
		{
			tracer.Trace (TraceEventType.Verbose, exception, message);
		}

		/// <summary>
		/// Traces an event of type <see cref="TraceEventType.Verbose"/> with the given exception, format string and arguments.
		/// </summary>
		public static void Verbose (this ITracer tracer, Exception exception, string format, params object[] args)
		{
			tracer.Trace (TraceEventType.Verbose, exception, format, args);
		}

		#endregion
	}
}
