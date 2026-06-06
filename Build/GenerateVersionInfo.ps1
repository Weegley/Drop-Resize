param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectDir,

    [Parameter(Mandatory = $true)]
    [string]$BaseVersion,

    [Parameter(Mandatory = $true)]
    [string]$Revision
)

$ErrorActionPreference = "Stop"

$generatedPath = Join-Path $ProjectDir "Properties\VersionInfo.Generated.cs"

if (-not ($Revision -match '^\d+$')) {
    throw "Revision must be a non-negative integer."
}

$version = "$BaseVersion.$Revision"
$content = @"
using System.Reflection;

[assembly: AssemblyVersion("$version")]
[assembly: AssemblyFileVersion("$version")]
[assembly: AssemblyInformationalVersion("$version")]
"@

Set-Content -LiteralPath $generatedPath -Value $content -Encoding ASCII
Write-Host "Generated Drop&Resize version $version"
