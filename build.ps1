$msbuilds = @(get-command msbuild -ea SilentlyContinue)
if ($msbuilds.Count -eq 0) {
    throw "MSBuild could not be found in the path. Please ensure MSBuild is in the path."
}

if (!(test-path ".\.nuget\NuGet.exe")) {
	if (!(test-path ".\.nuget")) {
		mkdir .nuget | out-null
	}
	write-host Downloading NuGet.exe...
    invoke-webrequest "https://nuget.org/nuget.exe" -outfile ".\.nuget\NuGet.exe"
}

if (test-path ".\packages.config") {
	write-host Installing root-level NuGet packages...
    .\.nuget\NuGet.exe Install -OutputDirectory packages -ExcludeVersion
}

$msbuild = $msbuilds[0].Definition
$maxCpuCount = $Env:MSBuildProcessorCount

# The defaults we pass to msbuild here can be overriden by $args :)
& $msbuild build.proj /m$maxCpuCount /nologo /p:DeployExtension=false /p:WarningLevel=0 /v:m $args