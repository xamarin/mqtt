using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Hermes.Flows;
using Hermes.Packets;
using Hermes.Properties;

namespace Hermes
{
	public class ClientPacketListener : IPacketListener
	{
		IDisposable firstPacketSubscription;
		IDisposable nextPacketsSubscription;
		IDisposable allPacketsSubscription;
		IDisposable senderSubscription;

		readonly IProtocolFlowProvider flowProvider;
		readonly ProtocolConfiguration configuration;
		readonly ReplaySubject<IPacket> packets;

		public ClientPacketListener (IProtocolFlowProvider flowProvider, ProtocolConfiguration configuration)
		{
			this.flowProvider = flowProvider;
			this.configuration = configuration;
			this.packets = new ReplaySubject<IPacket> (window: TimeSpan.FromSeconds(configuration.WaitingTimeoutSecs));
		}

		public IObservable<IPacket> Packets { get { return this.packets; } }

		public void Listen (IChannel<IPacket> channel)
		{
			var clientId = string.Empty;

			this.firstPacketSubscription = channel.Receiver
				.FirstOrDefaultAsync()
				.Subscribe(async packet => {
					if (packet == default (IPacket)) {
						return;
					}

					var connectAck = packet as ConnectAck;

					if (connectAck == null) {
						this.NotifyError (Resources.ClientPacketListener_FirstReceivedPacketMustBeConnectAck);
						return;
					}

					if (this.configuration.KeepAliveSecs > 0) {
						this.MaintainKeepAlive (channel);
					}

					await this.DispatchPacketAsync (packet, clientId, channel);
				}, ex => {
					this.NotifyError (ex);
				});

			this.nextPacketsSubscription = channel.Receiver
				.Skip(1)
				.Subscribe (async packet => {
					await this.DispatchPacketAsync (packet, clientId, channel);
				}, ex => {
					this.NotifyError (ex);
				});

			this.allPacketsSubscription = channel.Receiver.Subscribe (_ => { }, () => {
				this.packets.OnCompleted ();	
			});

			this.senderSubscription = channel.Sender
				.OfType<Connect> ()
				.FirstAsync ()
				.Subscribe (connect => {
					clientId = connect.ClientId;
				});
		}

		private void MaintainKeepAlive(IChannel<IPacket> channel)
		{
			channel.Sender
				.Timeout (new TimeSpan (0, 0, this.configuration.KeepAliveSecs))
				.Subscribe(_ => {}, async ex => {
					if (ex is TimeoutException) {
						var ping = new PingRequest ();

						await channel.SendAsync(ping);
					} else {
						this.NotifyError (ex);
					}
				});
		}

		private async Task DispatchPacketAsync(IPacket packet, string clientId, IChannel<IPacket> channel)
		{
			var flow = this.flowProvider.GetFlow (packet.Type);

			if (flow != null) {
				try {
					this.packets.OnNext (packet);

					await flow.ExecuteAsync (clientId, packet, channel);
				} catch (Exception ex) {
					this.NotifyError (ex);
				}
			}
		}

		private void NotifyError(Exception exception)
		{
			this.packets.OnError (exception);
		}

		private void NotifyError(string message)
		{
			this.NotifyError (new ProtocolException (message));
		}

		private void NotifyError(string message, Exception exception)
		{
			this.NotifyError (new ProtocolException (message, exception));
		}
	}
}
