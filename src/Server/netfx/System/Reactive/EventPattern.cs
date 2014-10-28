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
    /// <summary>
    /// Factory class for <see cref="IEventPattern{TEvent}"/>.
    /// </summary>
    ///	<nuget id="netfx-System.Reactive.EventStream.Interfaces" />
    static partial class EventPattern
    {
        /// <summary>
        /// Creates an event pattern instance for the given sender and event argument value.
        /// </summary>
        public static IEventPattern<TEvent> Create<TEvent>(object sender, TEvent @event)
        {
            Guard.NotNull(() => sender, sender);
            Guard.NotNull(() => @event, @event);

            if (typeof(TEvent) == @event.GetType())
                return new EventPatternImpl<TEvent>(sender, @event);
            else
                return (IEventPattern<TEvent>)Activator.CreateInstance(
                    typeof(EventPatternImpl<>).MakeGenericType(@event.GetType()), sender, @event);
        }

        private class EventPatternImpl<TEvent> : IEventPattern<TEvent>
        {
            public EventPatternImpl(object sender, TEvent args)
            {
                this.Sender = sender;
                this.EventArgs = args;
            }

            public object Sender { get; private set; }
            public TEvent EventArgs { get; private set; }
        }
    }
}
