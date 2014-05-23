/*
   Copyright 2014 NETFX

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0
*/

namespace Tests
{
	using System.IO;
	using System.Text.RegularExpressions;
	using Xunit;

	public class VersionInfoSpec
	{
		[Fact]
		public void when_parsing_version_info_then_can_retrieve_versions()
		{
			var version = @"Assembly=0.1.1
File=0.1.0
Package=0.1.0-pre";

			var assembly = Regex.Match(version, "(?<=Assembly=).*$", RegexOptions.Multiline).Value.Trim();
			var file = Regex.Match(version, "(?<=File=).*$", RegexOptions.Multiline).Value.Trim();
			var package = Regex.Match(version, "(?<=Package=).*$", RegexOptions.Multiline).Value.Trim();

			Assert.Equal("0.1.1", assembly);
			Assert.Equal("0.1.0", file);
			Assert.Equal("0.1.0-pre", package);
		}

		[Fact]
		public void when_parsing_version_then_can_read_from_string()
		{
			var Version = "1.0.0-pre";
			var Target = "out.txt";

			var assembly = Version.IndexOf('-') != -1 ?
				Version.Substring(0, Version.IndexOf('-')) :
				Version;

			File.WriteAllText(Target, string.Format(
@"AssemblyVersion={0}, 
FileVersion={0},
PackageVersion={1}", assembly, Version));

		}
	}
}
