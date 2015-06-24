#region BSD License
/* 
Copyright (c) 2011, Clarius Consulting
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, 
are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this list 
  of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice, this 
  list of conditions and the following disclaimer in the documentation and/or other 
  materials provided with the distribution.

* Neither the name of Clarius Consulting nor the names of its contributors may be 
  used to endorse or promote products derived from this software without specific 
  prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY 
EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES 
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT 
SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, 
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED 
TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR 
BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN 
ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH 
DAMAGE.
*/
#endregion

namespace System.Net.Mqtt.Diagnostics
{
    using System;
    using System.Diagnostics;

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
        public static void Critical(this ITracer tracer, object message)
        {
            tracer.Trace(TraceEventType.Critical, message);
        }

        /// <summary>
        /// Traces an event of type <see cref="TraceEventType.Critical"/> with the given format string and arguments.
        /// </summary>
        public static void Critical(this ITracer tracer, string format, params object[] args)
        {
            tracer.Trace(TraceEventType.Critical, format, args);
        }

        /// <summary>
        /// Traces an event of type <see cref="TraceEventType.Critical"/> with the given exception and message.
        /// </summary>
        public static void Critical(this ITracer tracer, Exception exception, object message)
        {
            tracer.Trace(TraceEventType.Critical, exception, message);
        }

        /// <summary>
        /// Traces an event of type <see cref="TraceEventType.Critical"/> with the given exception, format string and arguments.
        /// </summary>
        public static void Critical(this ITracer tracer, Exception exception, string format, params object[] args)
        {
            tracer.Trace(TraceEventType.Critical, exception, format, args);
        }

        #endregion

        #region Error overloads

        /// <summary>
        /// Traces an event of type <see cref="TraceEventType.Error"/> with the given message;
        /// </summary>
        public static void Error(this ITracer tracer, object message)
        {
            tracer.Trace(TraceEventType.Error, message);
        }

        /// <summary>
        /// Traces an event of type <see cref="TraceEventType.Error"/> with the given format string and arguments.
        /// </summary>
        public static void Error(this ITracer tracer, string format, params object[] args)
        {
            tracer.Trace(TraceEventType.Error, format, args);
        }

        /// <summary>
        /// Traces an event of type <see cref="TraceEventType.Error"/> with the given exception and message.
        /// </summary>
        public static void Error(this ITracer tracer, Exception exception, object message)
        {
            tracer.Trace(TraceEventType.Error, exception, message);
        }

        /// <summary>
        /// Traces an event of type <see cref="TraceEventType.Error"/> with the given exception, format string and arguments.
        /// </summary>
        public static void Error(this ITracer tracer, Exception exception, string format, params object[] args)
        {
            tracer.Trace(TraceEventType.Error, exception, format, args);
        }

        #endregion

        #region Warn overloads

        /// <summary>
        /// Traces an event of type <see cref="TraceEventType.Warning"/> with the given message;
        /// </summary>
        public static void Warn(this ITracer tracer, object message)
        {
            tracer.Trace(TraceEventType.Warning, message);
        }

        /// <summary>
        /// Traces an event of type <see cref="TraceEventType.Warning"/> with the given format string and arguments.
        /// </summary>
        public static void Warn(this ITracer tracer, string format, params object[] args)
        {
            tracer.Trace(TraceEventType.Warning, format, args);
        }

        /// <summary>
        /// Traces an event of type <see cref="TraceEventType.Warning"/> with the given exception and message.
        /// </summary>
        public static void Warn(this ITracer tracer, Exception exception, object message)
        {
            tracer.Trace(TraceEventType.Warning, exception, message);
        }

        /// <summary>
        /// Traces an event of type <see cref="TraceEventType.Warning"/> with the given exception, format string and arguments.
        /// </summary>
        public static void Warn(this ITracer tracer, Exception exception, string format, params object[] args)
        {
            tracer.Trace(TraceEventType.Warning, exception, format, args);
        }

        #endregion

        #region Info overloads

        /// <summary>
        /// Traces an event of type <see cref="TraceEventType.Information"/> with the given message;
        /// </summary>
        public static void Info(this ITracer tracer, object message)
        {
            tracer.Trace(TraceEventType.Information, message);
        }

        /// <summary>
        /// Traces an event of type <see cref="TraceEventType.Information"/> with the given format string and arguments.
        /// </summary>
        public static void Info(this ITracer tracer, string format, params object[] args)
        {
            tracer.Trace(TraceEventType.Information, format, args);
        }

        /// <summary>
        /// Traces an event of type <see cref="TraceEventType.Information"/> with the given exception and message.
        /// </summary>
        public static void Info(this ITracer tracer, Exception exception, object message)
        {
            tracer.Trace(TraceEventType.Information, exception, message);
        }

        /// <summary>
        /// Traces an event of type <see cref="TraceEventType.Information"/> with the given exception, format string and arguments.
        /// </summary>
        public static void Info(this ITracer tracer, Exception exception, string format, params object[] args)
        {
            tracer.Trace(TraceEventType.Information, exception, format, args);
        }

        #endregion

        #region Verbose overloads

        /// <summary>
        /// Traces an event of type <see cref="TraceEventType.Verbose"/> with the given message;
        /// </summary>
        public static void Verbose(this ITracer tracer, object message)
        {
            tracer.Trace(TraceEventType.Verbose, message);
        }

        /// <summary>
        /// Traces an event of type <see cref="TraceEventType.Verbose"/> with the given format string and arguments.
        /// </summary>
        public static void Verbose(this ITracer tracer, string format, params object[] args)
        {
            tracer.Trace(TraceEventType.Verbose, format, args);
        }

        /// <summary>
        /// Traces an event of type <see cref="TraceEventType.Verbose"/> with the given exception and message.
        /// </summary>
        public static void Verbose(this ITracer tracer, Exception exception, object message)
        {
            tracer.Trace(TraceEventType.Verbose, exception, message);
        }

        /// <summary>
        /// Traces an event of type <see cref="TraceEventType.Verbose"/> with the given exception, format string and arguments.
        /// </summary>
        public static void Verbose(this ITracer tracer, Exception exception, string format, params object[] args)
        {
            tracer.Trace(TraceEventType.Verbose, exception, format, args);
        }

        #endregion
    }
}
