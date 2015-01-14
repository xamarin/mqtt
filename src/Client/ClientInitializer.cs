using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Registration;
using System.Net.Sockets;
using System.Reflection;
using Hermes.Flows;
using Hermes.Storage;

namespace Hermes
{
	public class ClientInitializer : IInitalizer<Client>
	{
		readonly string hostAddress;
		CompositionContainer container;

		public ClientInitializer (string hostAddress)
		{
			this.hostAddress = hostAddress;
		}

		public Client Initialize (ProtocolConfiguration configuration)
		{
			var builder = this.RegisterDependencies ();
			var catalog = new AggregateCatalog ();

			catalog.Catalogs.Add (new AssemblyCatalog (typeof (IPacketManager).Assembly, builder));
			catalog.Catalogs.Add (new AssemblyCatalog (Assembly.GetExecutingAssembly(), builder));

			this.container = new CompositionContainer (catalog, CompositionOptions.DisableSilentRejection);

			var client = new TcpClient(this.hostAddress, configuration.Port);

			container.ComposeExportedValue<ProtocolConfiguration> (configuration);
			container.ComposeExportedValue<TcpClient> (client);
			container.ComposeParts (this);

			return this.container.GetExportedValue<Client> ();
		}

		private RegistrationBuilder RegisterDependencies()
		{
			var builder = new RegistrationBuilder ();

			builder.ForType<PacketBuffer> ().Export<IPacketBuffer> ();
			builder.ForType<TcpChannel>().Export<IChannel<byte[]>>();
			builder.ForType<TopicEvaluator> ().Export<ITopicEvaluator> ();
			builder.ForType<PacketChannelFactory> ().Export<IPacketChannelFactory> ();
			builder.ForType<InMemoryRepositoryProvider> ().Export<IRepositoryProvider> ();
			builder.ForType<ClientProtocolFlowProvider> ().Export<IProtocolFlowProvider> ();
			builder.ForType<ClientPacketChannelAdapter> ().Export<IPacketChannelAdapter> ();
			builder.ForType<Client> ().Export<Client> ();

			return builder;
		}
	}
}
