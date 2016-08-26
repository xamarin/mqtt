using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;

namespace System.Net.Mqtt.Server
{
    internal class EventStream : IEventStream
    {
        readonly ConcurrentDictionary<TypeInfo, SubjectWrapper> subjects = new ConcurrentDictionary<TypeInfo, SubjectWrapper>();
        readonly ConcurrentDictionary<TypeInfo, SubjectWrapper[]> compatibleSubjects = new ConcurrentDictionary<TypeInfo, SubjectWrapper[]>();
        readonly HashSet<object> observables;

        public EventStream ()
            : this (Enumerable.Empty<object> ())
        {
        }

        public EventStream (params object[] observables)
            : this ((IEnumerable<object>)observables)
        {
        }

        public EventStream (IEnumerable<object> observables)
        {
            this.observables = new HashSet<object> (observables);
        }

        public virtual void Push<TEvent> (TEvent @event)
        {
            if (@event == null) throw new ArgumentNullException (nameof (@event));

            if (!IsValid<TEvent> ())
                throw new NotSupportedException ();

            var eventType = @event.GetType().GetTypeInfo();

            InvokeCompatibleSubjects (@eventType, @event);
        }

        public virtual IObservable<TEvent> Of<TEvent> ()
        {
            if (!IsValid<TEvent> ())
                throw new NotSupportedException ();

            var subject = (IObservable<TEvent>)subjects.GetOrAdd (typeof (TEvent).GetTypeInfo(), info => {
				compatibleSubjects.Clear ();

                return new SubjectWrapper<TEvent> ();
            });

            var compatibleObservables = new [] { subject }.Concat(GetObservables<TEvent>()).ToArray();

            if (compatibleObservables.Length == 1)
                return compatibleObservables[0];

            return Observable.Merge (compatibleObservables);
        }

        protected virtual IEnumerable<IObservable<TEvent>> GetObservables<TEvent> ()
        {
            return observables.OfType<IObservable<TEvent>> ();
        }

        void InvokeCompatibleSubjects (TypeInfo info, object @event)
        {
            var compatible = compatibleSubjects.GetOrAdd(info, eventType => subjects.Keys
                .Where(subjectEventType => subjectEventType.IsAssignableFrom(eventType))
                .Select(subjectEventType => subjects[subjectEventType])
                .ToArray());

            foreach (var subject in compatible) {
                subject.OnNext (@event);
            }
        }

        static bool IsValid<TEvent> () => typeof (TEvent).GetTypeInfo ().IsPublic || typeof (TEvent).GetTypeInfo ().IsNestedPublic;
    }
}
