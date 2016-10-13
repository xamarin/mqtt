#pragma warning disable 0436
/*
   Copyright 2014 MobileEssentials

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0
*/

using System.Reflection;
using System.Net.Mqtt;

[assembly: AssemblyCompany("MobileEssentials")]
[assembly: AssemblyCopyright("Copyright © MobileEssentials 2014")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

#if DEBUG
[assembly: AssemblyConfiguration("DEBUG")]
#endif
#if RELEASE
[assembly: AssemblyConfiguration("RELEASE")]
#endif

[assembly: AssemblyVersion(ThisAssembly.Version)]
[assembly: AssemblyFileVersion(ThisAssembly.FileVersion)]
[assembly: AssemblyInformationalVersion(ThisAssembly.InformationVersion)]

namespace System.Net.Mqtt
{
	partial class ThisAssembly
	{
		public const string PublicKey = "002400000480000094000000060200000024000052534131000400000100010065b8df7c05d8bc2ba727492ad269e444ac8823b4c573a2b1a5f2aaec8cad859a5cf93a5d3dfb13a0632217f97f8c6bc27669440c1d18926320f63d406c3c8fb586f3481a62b18d45b506d956ac4e43b450c4afd028f68ead13d96e454d20d99b6ca5703ca401ac82e47058748f08c6dc01476596c599011fd14e74778c8652f0";
		public const string Version = ThisAssembly.Git.SemVer.Major + "." + ThisAssembly.Git.SemVer.Minor + "." + ThisAssembly.Git.SemVer.Patch;
		public const string FileVersion = Version;
		public const string InformationVersion = Version + ThisAssembly.Git.SemVer.DashLabel + "-" + ThisAssembly.Git.Branch + "+" + ThisAssembly.Git.Commit;
	}
}
#pragma warning restore 0436
