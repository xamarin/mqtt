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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Reactive.Subjects;

namespace System.Reactive
{
	/// <summary>
	/// Provides the implementation for a reactive extensions event stream, 
	/// allowing trending and analysis queries to be performed in real-time 
	/// over the events pushed through the stream.
	/// </summary>
	///	<nuget id="netfx-System.Reactive.EventStream.Implementation" />
	///	<remarks>
	///	The <see cref="IEventStream"/> interface implemented by this class is 
	///	provided by the nuget <c>netfx-System.Reactive.EventStream.Interfaces</c>, 
	///	which must be installed in the same project or one referenced by it. 
	/// </remarks>
	partial class EventStream : IEventStream
	{
		private ConcurrentDictionary<Type, object> subjects = new ConcurrentDictionary<Type, object>();

		/// <summary>
		/// Pushes an event to the stream, causing any analytics 
		/// subscriber to be invoked if appropriate.
		/// </summary>
		public void Push<TEvent>(TEvent @event)
		{
			Guard.NotNull(() => @event, @event);

			var subject = this.subjects.Find(@event.GetType()) as Subject<TEvent>;
			if (subject != null)
				subject.OnNext(@event);
		}

		/// <summary>
		/// Observes the events of a given type.
		/// </summary>
		public IObservable<TEvent> Of<TEvent>()
		{
			return (IObservable<TEvent>)subjects.GetOrAdd(typeof(TEvent), type => new Subject<TEvent>());
		}
	}
}
