namespace System.Net.Mqtt.Server
{
    internal interface IEventStream
    {
        void Push<TEvent> (TEvent args);

        IObservable<TEvent> Of<TEvent> ();
    }
}
