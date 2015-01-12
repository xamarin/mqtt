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
		readonly IPacketChannelFactory channelFactory;
		readonly IPacketChannelAdapter channelAdapter;
		readonly IConnectionProvider connectionProvider;
		readonly ProtocolConfiguration configuration;

		readonly IList<IChannel<IPacket>> channels = new List<IChannel<IPacket>> ();

		public Server (
			IObservable<IChannel<byte[]>> binaryChannelProvider, 
			IPacketChannelFactory channelFactory, 
			IPacketChannelAdapter channelAdapter,
			IConnectionProvider connectionProvider,
			ProtocolConfiguration configuration)
		{
			this.binaryChannelProvider = binaryChannelProvider;
			this.channelFactory = channelFactory;
			this.channelAdapter = channelAdapter;
			this.connectionProvider = connectionProvider;
			this.configuration = configuration;
		}

		public int ActiveChannels { get { return this.channels.Where(c => c.IsConnected).Count(); } }

		public IEnumerable<string> ActiveClients { get { return this.connectionProvider.ActiveClients; } }

		public void Start()
		{
			this.binaryChannelProvider.Subscribe (
				binaryChannel => this.ProcessChannel(binaryChannel), 
				ex => { tracer.Error (ex); }, 
				() => {}	
			);
		}

		public void Stop ()
		{
			this.Dispose (true);
			GC.SuppressFinalize (this);
		}

		void IDisposable.Dispose ()
		{
			this.Stop ();
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
			var clientId = string.Empty;

			var packetChannel = this.channelFactory.Create (binaryChannel);
			var protocolChannel = this.channelAdapter.Adapt (packetChannel);

			protocolChannel.Sender.Subscribe (_ => {
			}, ex => {
				tracer.Error (ex);
				this.CloseChannel (protocolChannel);
			}, () => {
				this.CloseChannel (protocolChannel);	
			});

			protocolChannel.Receiver.Subscribe (_ => {
			}, ex => { 
				tracer.Error (ex);
				this.CloseChannel (protocolChannel);
			}, () => { 
				this.CloseChannel (protocolChannel);
			});

			this.channels.Add (protocolChannel);
		}

		private void CloseChannel(IChannel<IPacket> channel)
		{
			this.channels.Remove (channel);
			channel.Dispose ();
		}
	}
}
