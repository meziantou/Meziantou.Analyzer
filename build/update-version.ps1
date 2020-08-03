﻿$VerbosePreference="Continue"
$ErrorActionPreference="Stop"

$version = $args[0]
if (!$version) {
    $version = "0.0.0"
}

Write-Output "Version: $version"

# Update NuGet package version
$FullPath = Resolve-Path $PSScriptRoot\..\src\Meziantou.Analyzer\Meziantou.Analyzer.csproj
Write-Output $FullPath
[xml]$content = Get-Content $FullPath
$packageVersion = Select-Xml -Xml $content -XPath /Project/PropertyGroup/PackageVersion
$packageVersion.Node.InnerText = $version
$content.Save($FullPath)

# Update VSIX version
$FullPath = Resolve-Path $PSScriptRoot\..\src\Meziantou.Analyzer.Vsix\source.extension.vsixmanifest
Write-Output $FullPath
[xml]$content = Get-Content $FullPath
$content.PackageManifest.Metadata.Identity.Version = $version
$content.Save($FullPath)
