using System.Diagnostics;
using Hermes.Diagnostics;
using IntegrationTests;

namespace Hermes
{
	public static class Tracing
	{
		public static void Initialize(ProtocolConfiguration configuration)
		{
			if (configuration.TracingEnabled) {
				Tracer.Manager.AddListener ("Hermes", new HermesTraceListener ());
				Tracer.Manager.SetTracingLevel ("Hermes", SourceLevels.All);
			}
		}
	}
}
