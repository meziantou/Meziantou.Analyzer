$PersonalAccessToken = $args[0]
$VsixPath = "src\Meziantou.Analyzer.Vsix\bin\Release\Meziantou.Analyzer.vsix"
$ManifestPath = "src\Meziantou.Analyzer.Vsix\bin\Release\extension-manifest.json"

$Installation = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -format json | ConvertFrom-Json
$Path = $Installation.installationPath

Write-Host $Path
$VsixPublisher = Join-Path -Path $Path -ChildPath "VSSDK\VisualStudioIntegration\Tools\Bin\VsixPublisher.exe" -Resolve

Write-Host $VsixPublisher
Test-Path $VsixPublisher

& $VsixPublisher publish -payload $VsixPath -publishManifest $ManifestPath -personalAccessToken $PersonalAccessToken -ignoreWarnings "VSIXValidatorWarning01,VSIXValidatorWarning02,VSIXValidatorWarning08"
