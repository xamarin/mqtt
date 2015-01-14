using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Registration;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hermes.Flows;
using Hermes.Storage;

namespace Hermes
{
	public class ServerInitializer : IInitalizer<Server>
	{
		CompositionContainer container;

		public Server Initialize(ProtocolConfiguration configuration)
		{
			var builder = this.RegisterDependencies ();
			var catalog = new AggregateCatalog ();

			catalog.Catalogs.Add (new AssemblyCatalog (typeof (IPacketManager).Assembly, builder));
			catalog.Catalogs.Add (new AssemblyCatalog (Assembly.GetExecutingAssembly(), builder));

			this.container = new CompositionContainer (catalog, CompositionOptions.DisableSilentRejection);

			var listener = new TcpListener(IPAddress.Any, configuration.Port);

			listener.Start ();

			var socketProvider = Observable
				.FromAsync (() => {
					return Task.Factory.FromAsync<TcpClient> (listener.BeginAcceptTcpClient, 
						listener.EndAcceptTcpClient, TaskCreationOptions.AttachedToParent);
				})
				.Repeat ()
				.Select (client => new TcpChannel (client, new PacketBuffer (), configuration));

			container.ComposeExportedValue<ProtocolConfiguration> (configuration);
			container.ComposeExportedValue<IObservable<IChannel<byte[]>>> (socketProvider);
			container.ComposeParts (this);

			return this.container.GetExportedValue<Server> ();
		}

		private RegistrationBuilder RegisterDependencies()
		{
			var builder = new RegistrationBuilder ();

			builder.ForType<TopicEvaluator> ().Export<ITopicEvaluator> ();
			builder.ForType<PacketChannelFactory> ().Export<IPacketChannelFactory> ();
			builder.ForType<InMemoryRepositoryProvider> ().Export<IRepositoryProvider> ();
			builder.ForType<ConnectionProvider> ().Export<IConnectionProvider> ();
			builder.ForType<ServerProtocolFlowProvider> ().Export<IProtocolFlowProvider> ();
			builder.ForType<ServerPacketChannelAdapter> ().Export<IPacketChannelAdapter> ();
			builder.ForType<Server> ().Export<Server> ();

			return builder;
		}
	}
}
