#pragma warning disable 0436
using System.Net.Mqtt;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle ("System.Net.Mqtt")]
[assembly: AssemblyDescription ("Shared components for Mqtt clients.")]
[assembly: InternalsVisibleTo ("System.Net.Mqtt, PublicKey=" + ThisAssembly.PublicKey)]
[assembly: InternalsVisibleTo("Tests, PublicKey=" + ThisAssembly.PublicKey)]
[assembly: InternalsVisibleTo ("DynamicProxyGenAssembly2,PublicKey=" + ThisAssembly.PublicKey)]
#pragma warning restore 0436