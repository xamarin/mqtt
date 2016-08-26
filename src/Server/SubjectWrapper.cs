using System.Reactive.Subjects;

namespace System.Net.Mqtt.Server
{
    internal abstract class SubjectWrapper
    {
        public abstract void OnNext (object value);
    }

    internal class SubjectWrapper<T> : SubjectWrapper, ISubject<T>
    {
        ISubject<T> subject = new Subject<T>();

        public override void OnNext (object value)
        {
            subject.OnNext ((T)value);
        }

        void IObserver<T>.OnCompleted ()
        {
            subject.OnCompleted ();
        }

        void IObserver<T>.OnError (Exception error)
        {
            subject.OnError (error);
        }

        void IObserver<T>.OnNext (T value)
        {
            subject.OnNext (value);
        }

        IDisposable IObservable<T>.Subscribe (IObserver<T> observer)
        {
            return subject.Subscribe (observer);
        }
    }
}
