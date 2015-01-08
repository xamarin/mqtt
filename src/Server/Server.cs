using System;
using System.Collections.Generic;
using System.Linq;
using Hermes.Diagnostics;
using Hermes.Packets;

namespace Hermes
{
	public class Server : IDisposable
	{
		static readonly ITracer tracer = Tracer.Get<Server> ();

		readonly IObservable<IChannel<byte[]>> binaryChannelProvider;
		readonly ProtocolConfiguration configuration;
		readonly IPacketChannelFactory channelFactory;
		readonly IPacketChannelAdapter channelAdapter;

		readonly IList<IChannel<byte[]>> channels = new List<IChannel<byte[]>> ();
		readonly IList<string> activeClients = new List<string> ();

		public Server (
			IObservable<IChannel<byte[]>> binaryChannelProvider, 
			IPacketChannelFactory channelFactory, 
			IPacketChannelAdapter channelAdapter,
			ProtocolConfiguration configuration)
		{
			this.binaryChannelProvider = binaryChannelProvider;
			this.channelFactory = channelFactory;
			this.channelAdapter = channelAdapter;
			this.configuration = configuration;

			this.binaryChannelProvider.Subscribe (
				binaryChannel => this.ProcessChannel(binaryChannel), 
				ex => { tracer.Error (ex); }, 
				() => {}	
			);
		}

		public int ActiveChannels { get { return this.channels.Count; } }

		public IEnumerable<string> ActiveClients { get { return this.activeClients; } }

		public void Close ()
		{
			this.Dispose (true);
			GC.SuppressFinalize (this);
		}

		void IDisposable.Dispose ()
		{
			this.Close ();
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposing) {
				foreach (var channel in channels) {
					channel.Dispose ();
				}
			}
		}

		private void ProcessChannel(IChannel<byte[]> binaryChannel)
		{
			this.channels.Add (binaryChannel);
			
			var clientId = string.Empty;

			var packetChannel = this.channelFactory.Create (binaryChannel);
			var protocolChannel = this.channelAdapter.Adapt (packetChannel);

			protocolChannel.Sender.Subscribe (_ => {
			}, ex => {
				tracer.Error (ex);
				this.CloseChannel (binaryChannel, clientId);
			}, () => {
				this.CloseChannel (binaryChannel, clientId);	
			});

			protocolChannel.Receiver.Subscribe (packet => {
				var connect = packet as Connect;
				if (connect != null)
					this.activeClients.Add (connect.ClientId);
			}, ex => { 
				tracer.Error (ex);
				this.CloseChannel (binaryChannel, clientId);
			}, () => { 
				this.CloseChannel (binaryChannel, clientId);
			});
		}

		private void CloseChannel(IChannel<byte[]> channel, string clientId)
		{
			this.RemoveClientId (clientId);

			this.channels.Remove (channel);
			channel.Dispose ();
		}

		private void RemoveClientId (string clientId)
		{
			if (!this.activeClients.Any (c => c == clientId))
				return;

			this.activeClients.Remove (clientId);
		}
	}
}
