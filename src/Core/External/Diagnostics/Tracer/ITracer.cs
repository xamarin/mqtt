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

namespace Hermes.Diagnostics
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Interface used by the application components to log messages.
    /// </summary>
    ///	<nuget id="Tracer.Interfaces" />
    partial interface ITracer
    {
        /// <summary>
        /// Traces the specified message with the given <see cref="TraceEventType"/>.
        /// </summary>
        void Trace(TraceEventType type, object message);

        /// <summary>
        /// Traces the specified formatted message with the given <see cref="TraceEventType"/>.
        /// </summary>
        void Trace(TraceEventType type, string format, params object[] args);

        /// <summary>
        /// Traces an exception with the specified message and <see cref="TraceEventType"/>.
        /// </summary>
        void Trace(TraceEventType type, Exception exception, object message);

        /// <summary>
        /// Traces an exception with the specified formatted message and <see cref="TraceEventType"/>.
        /// </summary>
        void Trace(TraceEventType type, Exception exception, string format, params object[] args);
    }
}
