#region BSD License
/* 
Copyright (c) 2011, NETFx
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

namespace System.Reactive
{
    using System;

    /// <summary>
    /// Provides an observable stream of events that 
    /// can be used for analysis or real-time handling.
    /// </summary>
    /// <remarks>
    /// Leveraging the Reactive Extensions (Rx), it's 
    /// possible to build fairly complicated event reaction 
    /// chains by simply issuing Linq-style queries over 
    /// the event stream. This is incredibly powerfull, 
    /// as explained in http://kzu.to/srVn3P. 
    /// <para>
    /// The stream supports two types of events: arbitrary 
    /// event payloads (not even restricted to inherit from 
    /// <see cref="EventArgs"/> as is usual in .NET) or 
    /// <see cref="IEventPattern{TEvent}"/>, which is an interface
    /// similar to the concrete implementation found on Rx. 
    /// The advantage of pushing the event pattern version is 
    /// that subscribers can perform additional filtering 
    /// if needed depending on the event sender. 
    /// </para>
    /// <para>
    /// See also <seealso cref="IEventStreamExtensions.Push"/>.
    /// </para>
    /// </remarks>
    ///	<nuget id="netfx-System.Reactive.EventStream.Interfaces" />
    partial interface IEventStream
    {
        /// <summary>
        /// Pushes an event to the stream, causing any 
        /// subscriber to be invoked if appropriate.
        /// </summary>
        void Push<TEvent>(TEvent @event);

        /// <summary>
        /// Pushes an event to the stream with its 
        /// sender information, causing any subscriber 
        /// to be invoked if appropriate.
        /// </summary>
        void Push<TEvent>(IEventPattern<TEvent> @event);

        /// <summary>
        /// Observes the events of a given type.
        /// </summary>
        IObservable<TEvent> Of<TEvent>();
    }
}