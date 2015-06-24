#pragma warning disable 0436
/*
   Copyright 2014 NETFX

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0
*/

using System.Reflection;
using System.Runtime.CompilerServices;
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
		public const string Version = ThisAssembly.Git.SemVer.Major + "." + ThisAssembly.Git.SemVer.Minor + "." + ThisAssembly.Git.SemVer.Patch;
		public const string FileVersion = Version;
		public const string InformationVersion = Version + ThisAssembly.Git.SemVer.DashLabel + "-" + ThisAssembly.Git.Branch + "+" + ThisAssembly.Git.Commit;
	}
}
#pragma warning restore 0436
