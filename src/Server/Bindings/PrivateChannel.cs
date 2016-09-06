using System.Diagnostics;
using System.Net.Mqtt.Exceptions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using ClientProperties = System.Net.Mqtt.Properties;

namespace System.Net.Mqtt.Server.Bindings
{
    internal class PrivateChannel : IMqttChannel<byte[]>
    {
        static readonly ITracer tracer = Tracer.Get<PrivateChannel>();

        bool disposed;

        readonly PrivateStream stream;
        readonly EndpointIdentifier identifier;
        readonly ReplaySubject<byte[]> receiver;
        readonly ReplaySubject<byte[]> sender;
        readonly IDisposable streamSubscription;

        public PrivateChannel (PrivateStream stream, EndpointIdentifier identifier, MqttConfiguration configuration)
        {
            this.stream = stream;
            this.identifier = identifier;
            receiver = new ReplaySubject<byte[]> (window: TimeSpan.FromSeconds(configuration.WaitingTimeoutSecs));
            sender = new ReplaySubject<byte[]> (window: TimeSpan.FromSeconds(configuration.WaitingTimeoutSecs));
            streamSubscription = SubscribeStream ();
        }

        public bool IsConnected => !stream.IsDisposed;

        public IObservable<byte[]> Receiver => receiver;

        public IObservable<byte[]> Sender => sender;

        public Task SendAsync (byte[] message)
        {
            if (disposed) {
                throw new ObjectDisposedException (nameof (PrivateChannel));
            }

            if (!IsConnected) {
                throw new MqttException (ClientProperties.Resources.MqttChannel_ClientNotConnected);
            }

            sender.OnNext (message);

            try {
                tracer.Verbose (ClientProperties.Resources.MqttChannel_SendingPacket, message.Length);

                stream.Send (message, identifier);

                return Task.FromResult (true);
            } catch (ObjectDisposedException disposedEx) {
                throw new MqttException (ClientProperties.Resources.MqttChannel_StreamDisconnected, disposedEx);
            }
        }

        public void Dispose ()
        {
            Dispose (disposing: true);
            GC.SuppressFinalize (this);
        }

        protected virtual void Dispose (bool disposing)
        {
            if (disposed) return;

            if (disposing) {
                tracer.Info (Properties.Resources.Mqtt_Disposing, nameof (PrivateChannel));

                streamSubscription.Dispose ();
                receiver.OnCompleted ();
                stream.Dispose ();

                disposed = true;
            }
        }

        IDisposable SubscribeStream ()
        {
            var senderIdentifier = identifier == EndpointIdentifier.Client ?
                EndpointIdentifier.Server :
                EndpointIdentifier.Client;

            return stream
                .ReceiveAsync (senderIdentifier)
                .ObserveOn (NewThreadScheduler.Default)
                .Subscribe (packet => {
                    tracer.Verbose(ClientProperties.Resources.MqttChannel_ReceivedPacket, packet.Length);

                    receiver.OnNext (packet);
                }, ex => {
                    if (ex is ObjectDisposedException) {
                        receiver.OnError (new MqttException (ClientProperties.Resources.MqttChannel_StreamDisconnected, ex));
                    } else {
                        receiver.OnError (ex);
                    }
                }, () => {
                    tracer.Warn (ClientProperties.Resources.MqttChannel_NetworkStreamCompleted);
                    receiver.OnCompleted ();
                });
        }
    }
}
