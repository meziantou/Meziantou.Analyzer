<#
.SYNOPSIS
Build the fuzzing harness, instrument the analyzers with SharpFuzz, and fuzz them with libFuzzer.

.DESCRIPTION
Requires Linux (or WSL), the SharpFuzz.CommandLine tool (dotnet tool install --global SharpFuzz.CommandLine),
and libfuzzer-dotnet (https://github.com/Metalnem/libfuzzer-dotnet/releases).
The findings are saved in <OutputDirectory>/findings, and the corpus to reuse in the next session in <OutputDirectory>/corpus-merged.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$LibFuzzer,
    [int]$DurationMinutes = 60,
    [int]$Workers = [Environment]::ProcessorCount,
    [int]$TimeoutSeconds = 30,
    [string]$OutputDirectory,
    [string]$PreviousCorpus,
    [int]$MergeMinutes = 30,
    [int]$MinimizeMinutes = 30,
    [string]$SharpFuzz = "sharpfuzz"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "../../..")).Path
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repositoryRoot "artifacts/fuzz"
}

$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
$LibFuzzer = (Resolve-Path $LibFuzzer).Path
$binDirectory = Join-Path $OutputDirectory "bin"

if (Test-Path $binDirectory) {
    Remove-Item -Recurse -Force $binDirectory
}

dotnet publish (Join-Path $PSScriptRoot "../Meziantou.Analyzer.Fuzz.csproj") --configuration Release --output $binDirectory

# Only the analyzers are instrumented, so the coverage that guides libFuzzer is the one of the rules, not the one of Roslyn
foreach ($assembly in "Meziantou.Analyzer.dll", "Meziantou.Analyzer.CodeFixers.dll") {
    Write-Host "Instrumenting $assembly"
    & $SharpFuzz (Join-Path $binDirectory $assembly)
}

$arguments = @(
    "campaign",
    "--libfuzzer", $LibFuzzer,
    "--tests", (Join-Path $repositoryRoot "tests/Meziantou.Analyzer.Test"),
    "--output", $OutputDirectory,
    "--duration", $DurationMinutes,
    "--workers", $Workers,
    "--timeout", $TimeoutSeconds,
    "--merge-minutes", $MergeMinutes,
    "--minimize-minutes", $MinimizeMinutes
)

if ($PreviousCorpus) {
    $arguments += @("--previous-corpus", [System.IO.Path]::GetFullPath($PreviousCorpus))
}

dotnet (Join-Path $binDirectory "Meziantou.Analyzer.Fuzz.dll") @arguments
